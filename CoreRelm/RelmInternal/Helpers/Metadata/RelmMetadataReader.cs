using CoreRelm.Attributes;
using CoreRelm.Exceptions;
using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Metadata
{
    internal sealed class RelmMetadataReader(IRelmDesiredSchemaBuilder desiredSchemaBuilder) : IRelmMetadataReader
    {
        private readonly IRelmDesiredSchemaBuilder _desiredSchemaBuilder = desiredSchemaBuilder;

        public RelmEntityDescriptor Describe(Type relmModelType)
        {
            if (relmModelType is null) 
                throw new ArgumentNullException(nameof(relmModelType));
            
            if (relmModelType.IsAbstract) 
                throw new ArgumentException("Type must be non-abstract.", nameof(relmModelType));

            if (!typeof(RelmModel).IsAssignableFrom(relmModelType))
                throw new ArgumentException($"Type '{relmModelType.FullName}' does not inherit CoreRelm.Models.RelmModel.");

            /* 
             * The implementation of this method is critical for your metadata pipeline. It must be robust and handle all the ways users might apply attributes to their model classes and properties.
             * The general approach is:
             * 1. Validate required class-level attributes (RelmDatabase, RelmTable) and extract database/table names.
             * 2. Collect all instance properties (including inherited) and identify those with [RelmColumn] to build column descriptors.
             * 3. For each column, check for related attributes like [RelmForeignKey] and [RelmIndex] to build FK and index descriptors.
             * 4. Handle navigation properties with [RelmForeignKey] that point to other entities, resolving their principal tables and columns.
             * 5. Ensure deterministic ordering of columns and other collections for consistent behavior.
             * 6. Return a fully populated RelmEntityDescriptor with all the metadata needed for schema generation and migrations.
             * 
             * As you implement, consider edge cases such as:
             * - Missing required attributes or properties
             * - Conflicting attribute configurations
             * - Inherited properties with attributes
             * - Navigation properties referencing other entities
             * - Composite keys (if supported)
             * /
            // Required class attributes
            var dbAttr = GetRequiredAttribute(relmModelType, nameof(RelmDatabase))
                ?? throw new MissingRelmDatabaseAttributeException(relmModelType);
            var tableAttr = GetRequiredAttribute(relmModelType, nameof(RelmTable))
                ?? throw new MissingRelmTableAttributeException(relmModelType);

            var databaseName = GetStringProperty(dbAttr, nameof(RelmDatabase.DatabaseName))
                ?? throw new InvalidOperationException($"[{dbAttr?.GetType().Name}] on {relmModelType.FullName} must expose DatabaseName.");
            var tableName = GetStringProperty(tableAttr, nameof(RelmTable.TableName))
                ?? throw new InvalidOperationException($"[{tableAttr.GetType().Name}] on {relmModelType.FullName} must expose TableName.");

            // Project to column descriptors
            var columns = new List<RelmColumnDescriptor>();
            var foreignKeyDescriptorList = new List<RelmForeignKeyDescriptor>();
            var indexDescriptorList = new List<RelmIndexDescriptor>();

            // Columns (including inherited)
            var members = GetAllInstanceProperties(relmModelType);

            var columnMembers = members
                .Select(p => (Prop: p, ColAttr: GetOptionalAttribute(p, nameof(RelmColumn))))
                .Where(x => x.ColAttr is not null)
                .ToList();

            int ordinal = 0;
            foreach (var (prop, colAttr) in columnMembers)
            {
                var col = colAttr!;
                var columnName = GetStringProperty(col, nameof(RelmColumn.ColumnName))
                                 ?? prop.Name;

                var storeType = GetStringProperty(col, nameof(RelmColumn.ColumnType))
                    ?? GetStringProperty(col, nameof(RelmColumn.ColumnDbType))
                    ?? throw new InvalidOperationException($"[RelmColumn] on {relmModelType.FullName}.{prop.Name} must define ColumnType.");

                var isNullable = GetBoolProperty(col, nameof(RelmColumn.IsNullable)) ?? true;

                // default SQL: may be null
                var defaultSql = GetStringProperty(col, nameof(RelmColumn.DefaultValue));

                var isPk = GetBoolProperty(col, nameof(RelmColumn.PrimaryKey)) ?? false;
                var isAuto = GetBoolProperty(col, nameof(RelmColumn.Autonumber)) ?? false;
                var isUnique = GetBoolProperty(col, nameof(RelmColumn.Unique)) ?? false;

                columns.Add(new RelmColumnDescriptor(
                    ColumnName: columnName,
                    StoreType: storeType,
                    IsNullable: isNullable,
                    DefaultSql: defaultSql,
                    IsPrimaryKey: isPk,
                    IsAutoIncrement: isAuto,
                    IsUnique: isUnique,
                    Ordinal: ordinal++
                ));

                // Foreign key attribute on the SAME property (common pattern)
                var fkAttr = GetOptionalAttribute(prop, nameof(RelmForeignKey));
                if (fkAttr is not null)
                {
                    // These property names are guesses; tighten once you confirm RelmForeignKey attribute API.
                    var principalTable = GetStringProperty(fkAttr, "PrincipalTable")
                                         ?? GetStringProperty(fkAttr, "ReferencedTable")
                                         ?? throw new InvalidOperationException($"[RelmForeignKey] on {relmModelType.FullName}.{prop.Name} must specify principal table.");

                    var principalColumn = GetStringProperty(fkAttr, "PrincipalColumn")
                                          ?? GetStringProperty(fkAttr, "ReferencedColumn")
                                          ?? "InternalId";

                    var onDelete = GetStringProperty(fkAttr, "OnDelete")
                                   ?? "CASCADE"; // default; adjust if your attribute provides it

                    var fkName = GetStringProperty(fkAttr, "Name")
                                 ?? $"FK_{tableName}_{columnName}";

                    foreignKeyDescriptorList.Add(new RelmForeignKeyDescriptor(
                        Name: fkName,
                        LocalColumns: [columnName],
                        PrincipalTable: principalTable,
                        PrincipalColumns: [principalColumn],
                        OnDelete: onDelete
                    ));
                }

                var indexAttr = GetOptionalAttribute(prop, nameof(RelmIndex));
                if (indexAttr is not null)
                {
                    var indexName = GetStringProperty(indexAttr, "Name")
                                    ?? $"IX_{tableName}_{columnName}";
                    var isUniqueIndex = GetBoolProperty(indexAttr, "IsUnique") ?? false;
                    indexDescriptorList.Add(new RelmIndexDescriptor(
                        Name: indexName,
                        Columns: [columnName],
                        IsUnique: isUniqueIndex
                    ));
                }
            }

            // Deterministic ordering
            columns = OrderColumnsDeterministically(columns);
            foreignKeyDescriptorList = [.. foreignKeyDescriptorList.OrderBy(x => x.Name, StringComparer.Ordinal)];
            indexDescriptorList = [.. indexDescriptorList.OrderBy(x => x.Name, StringComparer.Ordinal)];

            var navPropsWithFk = members
                .Select(p => (NavigationProperty: p, ForeignKey: p.GetCustomAttributes(true).OfType<RelmForeignKey>().FirstOrDefault()))
                .Where(x => x.ForeignKey != null)
                .ToList();

            foreach (var (navigationProperty, foreignKeyAttribute) in navPropsWithFk)
            {
                // principal type is the navigation property's type
                var principalType = navigationProperty.PropertyType;

                // require principalType is RelmModel (or at least has RelmTable/RelmDatabase)
                // (you can decide whether to allow collections later; likely skip for now)

                // validate arrays exist
                var localKeys = foreignKeyAttribute?.LocalKeys;
                var foreignKeys = foreignKeyAttribute?.ForeignKeys;

                /*
                if (localKeys == null || foreignKeys == null)
                    throw new InvalidOperationException($"RelmForeignKey on {clrType.FullName}.{navProp.Name} must define LocalKeys and ForeignKeys.");

                if (localKeys.Length != foreignKeys.Length)
                    throw new InvalidOperationException($"RelmForeignKey on {clrType.FullName}.{navProp.Name} has mismatched key counts: LocalKeys={localKeys.Length}, ForeignKeys={foreignKeys.Length}.");

                // resolve principal table name via metadata reader (recursive call or cached)
                var principalDesc = Describe(principalType); // ensure no infinite recursion via caching

                // optional: enforce same database (recommended for now)
                if (!string.Equals(principalDesc.DatabaseName, currentDesc.DatabaseName, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Cross-database foreign keys not supported: {currentDesc.TableName} ({currentDesc.DatabaseName}) -> {principalDesc.TableName} ({principalDesc.DatabaseName}).");

                // resolve local column names by locating dependent scalar properties by name
                // and reading their RelmColumn.ColumnName (or underscore fallback)
                var localColumnNames = new List<string>();
                for (var i = 0; i < localKeys.Length - 1; i++)
                {
                    var localScalarProp = FindPropertyByName(currentClrType, localKeys[i]);
                    var localColAttr = localScalarProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault();
                    if (localColAttr == null) 
                        throw new InvalidOperationException($"RelmForeignKey on {currentClrType.FullName}.{navProp.Name} references local key '{localKeys[i]}' which lacks a [RelmColumn] attribute.");

                    localColumnNames.Add(ResolveColumnName(localScalarProp, localColAttr));
                }

                // resolve foreign column names similarly on principal type
                var foreignColumnNames = new List<string>();
                for (var i = 0; i < foreignKeys.Length - 1; i++)
                {
                    var foreignScalarProp = FindPropertyByName(principalType, foreignKeys[i]);
                    var foreignColAttr = foreignScalarProp.GetCustomAttributes(true).OfType<RelmColumn>().FirstOrDefault();
                    if (foreignColAttr == null) 
                        throw new InvalidOperationException($"RelmForeignKey on {currentClrType.FullName}.{navProp.Name} references foreign key '{foreignKeys[i]}' which lacks a [RelmColumn] attribute.");

                    foreignColumnNames.Add(ResolveColumnName(foreignScalarProp, foreignColAttr));
                }
                * /

                // Build FK descriptor(s)
                // For composite keys: store all columns in one FK descriptor model (recommended)
                // If your descriptor currently supports only a single column, upgrade it to support IReadOnlyList<string>.
            }

            return new RelmEntityDescriptor(
                TableName: tableName,
                Columns: columns,
                Indexes: indexDescriptorList,
                ForeignKeys: foreignKeyDescriptorList,
                ClrType: relmModelType,
                DatabaseName: databaseName,
                Notes: null,
                Hints: new Dictionary<string, string>(StringComparer.Ordinal)
            );
            */
            var dbAttr = GetTypeAttribute(relmModelType, nameof(RelmDatabase))
                ?? throw new MissingRelmDatabaseAttributeException(relmModelType);
            var tableAttr = GetTypeAttribute(relmModelType, nameof(RelmTable))
                ?? throw new MissingRelmTableAttributeException(relmModelType);

            var databaseName = GetStringProperty(dbAttr, nameof(RelmDatabase.DatabaseName))
                ?? throw new InvalidOperationException($"[{dbAttr?.GetType().Name}] on {relmModelType.FullName} must expose DatabaseName.");
            var tableName = GetStringProperty(tableAttr, nameof(RelmTable.TableName))
                ?? throw new InvalidOperationException($"[{tableAttr.GetType().Name}] on {relmModelType.FullName} must expose TableName.");

            var schemaSnapshot = _desiredSchemaBuilder.BuildAsync(databaseName, [new ValidatedModelType(relmModelType, databaseName, tableName)])
                .GetAwaiter()
                .GetResult();

            return new RelmEntityDescriptor(
                TableName: tableName,
                Columns: schemaSnapshot.Tables[tableName].Columns.Values.Select(x => new RelmColumnDescriptor(
                    ColumnName: x.ColumnName,
                    StoreType: x.ColumnType,
                    IsNullable: x.IsNullable == "YES",
                    DefaultSql: x.DefaultValue,
                    IsPrimaryKey: x.IsPrimaryKey,
                    IsAutoIncrement: x.IsAutoIncrement,
                    IsUnique: x.IsUnique,
                    Ordinal: x.OrdinalPosition
                )).ToList(),
                Indexes: schemaSnapshot.Tables[tableName].Indexes.Values.Select(x => new RelmIndexDescriptor(
                    Name: x.IndexName,
                    IsUnique: !x.NonUnique,
                    Columns: x.Columns?.Select(c => c.ColumnName).ToList() ?? []
                )).ToList(),
                ForeignKeys: schemaSnapshot.Tables[tableName].ForeignKeys.Values.Select(x => new RelmForeignKeyDescriptor(
                    Name: x.ConstraintName,
                    LocalColumns: x.ColumnNames?.Where(y => y != null).Select(y => y!).ToList() ?? [],
                    PrincipalTable: x.TableName,
                    PrincipalColumns: x.ReferencedColumnNames?.Where(y => y != null).Select(y => y!).ToList() ?? [],
                    OnDelete: x.DeleteRule
                )).ToList(),
                ClrType: relmModelType,
                DatabaseName: databaseName,
                Notes: null,
                Hints: new Dictionary<string, string>(StringComparer.Ordinal)
            );
        }

        private static List<PropertyInfo> GetAllInstanceProperties(Type t)
        {
            // Include inherited properties, avoid duplicates (shadowing)
            var props = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

            var current = t;
            while (current != null && current != typeof(object))
            {
                foreach (var p in current.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    // Prefer derived declarations
                    if (!props.ContainsKey(p.Name))
                        props[p.Name] = p;
                }
                current = current.BaseType;
            }

            return props.Values.ToList();
        }

        private static Attribute? GetTypeAttribute(Type t, string shortName)
            => t.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static Attribute? GetMemberAttribute(MemberInfo member, string shortName)
            => member.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static string? GetStringProperty(Attribute? attribute, string propName)
        {
            if (attribute == null) 
                return null;
            
            var attributeProperty = attribute
                .GetType()
                .GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);

            if (attributeProperty == null)
                return null;

            return attributeProperty.PropertyType == typeof(string) 
                ? (string?)attributeProperty.GetValue(attribute) 
                : (typeof(Enum).IsAssignableFrom(Nullable.GetUnderlyingType(attributeProperty.PropertyType) ?? attributeProperty.PropertyType)
                    ? ((Enum?)attributeProperty.GetValue(attribute))?.ToString() 
                    : null);
        }

        private static bool? GetBoolProperty(Attribute? attr, string propName)
        {
            var p = attr.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p?.PropertyType == typeof(bool)) return (bool?)p.GetValue(attr);
            if (p?.PropertyType == typeof(bool?)) return (bool?)p.GetValue(attr);
            return null;
        }

        private static List<RelmColumnDescriptor> OrderColumnsDeterministically(List<RelmColumnDescriptor> cols)
        {
            // Base columns first in your desired order, then everything else by name.
            // Adjust these names if your base columns use different casing.
            var baseOrder = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = 0,
                ["active"] = 101,
                ["InternalId"] = 102,
                ["create_date"] = 103,
                ["last_updated"] = 104
            };

            return cols
                .OrderBy(c => baseOrder.TryGetValue(c.ColumnName, out var i) ? i : 1000)
                .ThenBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
