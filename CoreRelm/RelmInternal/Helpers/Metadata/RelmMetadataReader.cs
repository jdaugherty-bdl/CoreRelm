using CoreRelm.Attributes;
using CoreRelm.Exceptions;
using CoreRelm.Interfaces.Metadata;
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

    internal sealed class RelmMetadataReader : IRelmMetadataReader
    {
        public RelmEntityDescriptor Describe(Type relmModelType)
        {
            if (relmModelType is null) throw new ArgumentNullException(nameof(relmModelType));
            if (relmModelType.IsAbstract) throw new ArgumentException("Type must be non-abstract.", nameof(relmModelType));
            if (!typeof(RelmModel).IsAssignableFrom(relmModelType))
                throw new ArgumentException($"Type '{relmModelType.FullName}' does not inherit CoreRelm.Models.RelmModel.");

            // Required class attributes
            var dbAttr = GetRequiredAttribute(relmModelType, "RelmDatabase")
                         ?? throw new MissingRelmDatabaseAttributeException(relmModelType);
            var tableAttr = GetRequiredAttribute(relmModelType, "RelmTable")
                            ?? throw new MissingRelmTableAttributeException(relmModelType);

            var databaseName = GetStringProperty(dbAttr, "DatabaseName")
                ?? throw new InvalidOperationException($"[{dbAttr.GetType().Name}] on {relmModelType.FullName} must expose DatabaseName.");
            var tableName = GetStringProperty(tableAttr, "TableName")
                ?? throw new InvalidOperationException($"[{tableAttr.GetType().Name}] on {relmModelType.FullName} must expose TableName.");

            // Columns (including inherited)
            var members = GetAllInstanceProperties(relmModelType);

            var columnMembers = members
                .Select(p => (Prop: p, ColAttr: GetOptionalAttribute(p, "RelmColumn")))
                .Where(x => x.ColAttr is not null)
                .ToList();

            // Project to column descriptors
            // NOTE: We don’t assume your RelmColumn property names here beyond common ones.
            // If you want, we can tighten this once you confirm the RelmColumn attribute API.
            var columns = new List<RelmColumnDescriptor>(capacity: columnMembers.Count);
            var foreignKeyDescriptorList = new List<RelmForeignKeyDescriptor>();

            int ordinal = 0;

            foreach (var (prop, colAttr) in columnMembers)
            {
                var col = colAttr!;
                var columnName = GetStringProperty(col, "ColumnName")
                                 ?? GetStringProperty(col, "Name")
                                 ?? prop.Name;

                var storeType = GetStringProperty(col, "StoreType")
                                ?? GetStringProperty(col, "SqlType")
                                ?? throw new InvalidOperationException($"[RelmColumn] on {relmModelType.FullName}.{prop.Name} must define StoreType/SqlType.");

                var isNullable = GetBoolProperty(col, "IsNullable") ?? true;

                // default SQL: may be null
                var defaultSql = GetStringProperty(col, "DefaultSql")
                                 ?? GetStringProperty(col, "Default");

                var isPk = GetBoolProperty(col, "IsPrimaryKey") ?? false;
                var isAuto = GetBoolProperty(col, "IsAutoIncrement") ?? false;
                var isUnique = GetBoolProperty(col, "IsUnique") ?? false;

                // Enforce invariant: last_updated uses ON UPDATE semantics (no triggers)
                if (string.Equals(columnName, "last_updated", StringComparison.OrdinalIgnoreCase))
                {
                    // If your attribute already provides this, great; otherwise, enforce.
                    // MySQL uses: DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    defaultSql = "CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP";
                    isNullable = false;
                }

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
                var fkAttr = GetOptionalAttribute(prop, "RelmForeignKey");
                if (fkAttr is not null)
                {
                    // These property names are guesses; tighten once you confirm RelmForeignKey attribute API.
                    var principalTable = GetStringProperty(fkAttr, "PrincipalTable")
                                         ?? GetStringProperty(fkAttr, "ReferencedTable")
                                         ?? throw new InvalidOperationException($"[RelmForeignKey] on {relmModelType.FullName}.{prop.Name} must specify principal table.");

                    var principalColumn = GetStringProperty(fkAttr, "PrincipalColumn")
                                          ?? GetStringProperty(fkAttr, "ReferencedColumn")
                                          ?? "InternalId"; // your convention

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
            }

            // Deterministic ordering
            columns = OrderColumnsDeterministically(columns);
            foreignKeyDescriptorList = [.. foreignKeyDescriptorList.OrderBy(x => x.Name, StringComparer.Ordinal)];

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
                */

                // Build FK descriptor(s)
                // For composite keys: store all columns in one FK descriptor model (recommended)
                // If your descriptor currently supports only a single column, upgrade it to support IReadOnlyList<string>.
            }


            // Indexes: empty for now until you confirm the index attribute name(s)
            var indexes = Array.Empty<RelmIndexDescriptor>();

            return new RelmEntityDescriptor(
                TableName: tableName,
                Columns: columns,
                Indexes: indexes,
                ForeignKeys: foreignKeyDescriptorList,
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

        private static Attribute? GetRequiredAttribute(Type t, string shortName)
            => t.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static Attribute? GetOptionalAttribute(MemberInfo member, string shortName)
            => member.GetCustomAttributes(inherit: true).OfType<Attribute>()
                .FirstOrDefault(a => a.GetType().Name is var n &&
                                     (n.Equals(shortName, StringComparison.Ordinal) || n.Equals(shortName + "Attribute", StringComparison.Ordinal)));

        private static string? GetStringProperty(Attribute attr, string propName)
        {
            var p = attr.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            return p?.PropertyType == typeof(string) ? (string?)p.GetValue(attr) : null;
        }

        private static bool? GetBoolProperty(Attribute attr, string propName)
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
                ["active"] = 1,
                ["InternalId"] = 2,
                ["create_date"] = 3,
                ["last_updated"] = 4
            };

            return cols
                .OrderBy(c => baseOrder.TryGetValue(c.ColumnName, out var i) ? i : 1000)
                .ThenBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
