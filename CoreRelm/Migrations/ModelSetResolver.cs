using CoreRelm.Attributes;
using CoreRelm.Exceptions;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Models;
using CoreRelm.Models.Migrations;
using Mysqlx.Notice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{
    public sealed class ModelSetResolver : IModelSetResolver
    {
        public ModelSetsFile LoadModelSets(string? modelSetsPath)
        {
            if (string.IsNullOrWhiteSpace(modelSetsPath))
                throw new ArgumentException("No model sets path set.");

            if (!File.Exists(modelSetsPath))
                throw new FileNotFoundException($"modelsets.json not found: {modelSetsPath}");

            var json = File.ReadAllText(modelSetsPath);
            var data = JsonSerializer.Deserialize<ModelSetsFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (data is null)
                throw new InvalidOperationException("Failed to parse modelsets.json (deserialized null).");

            if (data.Version != 1)
                throw new NotSupportedException($"Unsupported modelsets.json version: {data.Version}");

            return data;
        }

        public ResolvedModelSet ResolveSet(ModelSetsFile file, string setName, Assembly modelsAssembly)
        {
            return ResolveSetAndDiagnostics(file, setName, modelsAssembly).Resolved;
        }

        /// <summary>
        /// Resolves a model set definition by name from the specified model sets file and returns the resolved set
        /// along with detailed diagnostics.
        /// </summary>
        /// <remarks>The diagnostics object provides detailed information about the resolution process,
        /// such as missing types, attribute validation errors, and counts of included or excluded types. This method
        /// does not throw for missing or invalid attributes; instead, such issues are reported in the
        /// diagnostics.</remarks>
        /// <param name="file">The model sets file containing the definitions to resolve.</param>
        /// <param name="setName">The name of the model set to resolve. Must match a set defined in the provided file.</param>
        /// <returns>A tuple containing the resolved model set and a diagnostics object with information about the resolution
        /// process, including any errors or warnings encountered.</returns>
        /// <exception cref="ArgumentException">Thrown if a model set with the specified name does not exist in the provided file.</exception>
        public (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetWithDiagnostics(ModelSetsFile file, string setName, Assembly modelsAssembly)
        {
            return ResolveSetAndDiagnostics(file, setName, modelsAssembly);
        }

        private (ResolvedModelSet Resolved, ResolvedModelSetDiagnostics Diagnostics) ResolveSetAndDiagnostics(ModelSetsFile file, string setName, Assembly modelsAssembly)
        {
            ArgumentNullException.ThrowIfNull(file);

            if (string.IsNullOrWhiteSpace(setName))
                throw new ArgumentException("No model set name specified.", nameof(setName));

            if (file.Sets is null || file.Sets.Count == 0)
                throw new ModelSetResolutionException("modelsets.json contains no sets.");

            if (!file.Sets.TryGetValue(setName, out var def) || def is null)
                throw new ModelSetNotFoundException(setName);

            // 1) Resolve explicit types
            var explicitTypeNames = (def.Types ?? [])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();
            var explicitTypeNameCount = explicitTypeNames.Count;

            var allTypes = SafeGetTypes(modelsAssembly);
            var byFullName = allTypes
                .Where(t => t.FullName is not null)
                .ToDictionary(t => t.FullName!, t => t, StringComparer.Ordinal);

            var explicitTypes = new List<Type>(explicitTypeNameCount);
            var errors = new List<string>();
            foreach (var typeName in explicitTypeNames)
            {
                var t = ResolveTypeByName(modelsAssembly, byFullName, typeName);
                if (t is null)
                {
                    errors.Add($"ERROR: Explicit type '{typeName}' not found in assembly '{modelsAssembly.FullName}' (set '{setName}').");
                    continue;
                }
                explicitTypes.Add(t);
            }

            var explicitTypesResolvedCount = explicitTypes.Count;

            // 2) Resolve namespace prefixes
            var prefixes = (def.NamespacePrefixes ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();

            var namespacePrefixCount = prefixes.Length;

            var assemblyTypeCount = allTypes.Count;

            var typesByPrefix = new Dictionary<string, IReadOnlyList<Type>>(StringComparer.Ordinal);
            var namespaceMatches = new List<Type>();
            if (prefixes.Length > 0)
            {
                foreach (var prefix in prefixes)
                {
                    var matches = allTypes
                        .Where(t => t.Namespace is not null && t.Namespace.StartsWith(prefix, StringComparison.Ordinal))
                        .ToList();

                    typesByPrefix[prefix] = matches;

                    if (matches.Count == 0)
                        errors.Add($"WARNING: Namespace prefix '{prefix}' did not match any types in assembly '{modelsAssembly.FullName}' (set '{setName}').");

                    namespaceMatches.AddRange(matches);
                }
            }

            var namespaceMatchedCount = namespaceMatches.Count;

            // 3) Union + filter to RelmModel subclasses
            var candidates = explicitTypes
                .Concat(namespaceMatches)
                .Distinct()
                .OrderBy(t => t.FullName, StringComparer.Ordinal)
                .ToList();

            if (candidates.Count == 0)
                throw new InvalidOperationException($"Resolved model set '{setName}' contains no RelmModel types.");

            var candidateCountBeforeFilter = candidates.Count;

            // filter counts
            var abstractExcludedCount = candidates.Count(t => t.IsAbstract);
            if (abstractExcludedCount > 0)
                errors.Add($"Excluded {abstractExcludedCount} abstract type(s): {string.Join(", ", candidates.Where(t => t.IsAbstract).Select(t => t.FullName))}");

            var notRelmModelExcludedCount = candidates.Count(t => !typeof(RelmModel).IsAssignableFrom(t));
            if (notRelmModelExcludedCount > 0)
                errors.Add($"Excluded {notRelmModelExcludedCount} non-RelmModel type(s): {string.Join(", ", candidates.Where(t => !typeof(RelmModel).IsAssignableFrom(t)).Select(t => t.FullName))}");

            var filtered = candidates
                .Where(t => !t.IsAbstract && typeof(RelmModel).IsAssignableFrom(t))
                .ToList();

            if (filtered.Count == 0)
                throw new ModelSetResolutionException($"Resolved set '{setName}' contains no RelmModel types after filtering.");

            // 4) Validate required attributes (RelmDatabase + RelmTable)
            int missingDb = 0, missingTable = 0, valueErrors = 0;

            var validated = new List<ValidatedModelType>();

            foreach (var t in filtered)
            {
                Attribute? dbAttr = null;
                Attribute? tableAttr = null;

                try
                {
                    dbAttr = GetOptionalAttribute(t, nameof(RelmDatabase));
                    tableAttr = GetOptionalAttribute(t, nameof(RelmTable));

                    if (dbAttr is null)
                    {
                        missingDb++;
                        errors.Add($"ERROR: {t.FullName} is missing required [RelmDatabase].");
                        continue;
                    }

                    if (tableAttr is null)
                    {
                        missingTable++;
                        errors.Add($"ERROR: {t.FullName} is missing required [RelmTable].");
                        continue;
                    }

                    var dbName = ExtractRelmDatabaseName(dbAttr);
                    var tableName = ExtractRelmTableName(tableAttr);

                    if (string.IsNullOrWhiteSpace(dbName))
                    {
                        valueErrors++;
                        errors.Add($"ERROR: {t.FullName} has [RelmDatabase] but DatabaseName is null/empty.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(tableName))
                    {
                        valueErrors++;
                        errors.Add($"ERROR: {t.FullName} has [RelmTable] but TableName is null/empty.");
                        continue;
                    }

                    validated.Add(new ValidatedModelType(t, dbName, tableName));
                }
                catch (Exception ex)
                {
                    valueErrors++;
                    errors.Add($"ERROR: {t.FullName} attribute read failed: {ex.Message}");
                }
            }

            // 5) Group by db
            var byDb = validated
                .GroupBy(v => v.DatabaseName, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            var resolved = new ResolvedModelSet(setName, validated, byDb, errors);

            var diag = new ResolvedModelSetDiagnostics(
                SetName: setName,
                AssemblyTypeCount: assemblyTypeCount,
                ExplicitTypeNameCount: explicitTypeNameCount,
                ExplicitTypesResolvedCount: explicitTypesResolvedCount,
                NamespacePrefixCount: namespacePrefixCount,
                NamespaceMatchedCount: namespaceMatchedCount,
                CandidateCountBeforeFilter: candidateCountBeforeFilter,
                AbstractExcludedCount: abstractExcludedCount,
                NotRelmModelExcludedCount: notRelmModelExcludedCount,
                IncludedCount: validated.Count,
                MissingRelmDatabaseCount: missingDb,
                MissingRelmTableCount: missingTable,
                AttributeValueErrorCount: valueErrors,
                Errors: errors
            );

            return (resolved, diag);
        }

        private static Type? ResolveTypeByName(Assembly asm, Dictionary<string, Type> index, string fullName)
        {
            // Fast path
            var t = asm.GetType(fullName, throwOnError: false, ignoreCase: false);
            if (t is not null) return t;

            // Fallback path (handles some edge cases)
            return index.TryGetValue(fullName, out var found) ? found : null;
        }

        private static List<Type> SafeGetTypes(Assembly asm)
        {
            try
            {
                return [.. asm.GetTypes()];
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Return what we can; loader exceptions are informational
                return [.. ex.Types.Where(t => t is not null).Cast<Type>()];
            }
        }

        private static Attribute? GetOptionalAttribute(Type t, string shortName)
        {
            var attrs = t.GetCustomAttributes(inherit: true).OfType<Attribute>();

            return attrs.FirstOrDefault(a =>
                a.GetType().Name.Equals(shortName, StringComparison.Ordinal) ||
                a.GetType().Name.Equals(shortName + "Attribute", StringComparison.Ordinal));
        }

        private static string? ExtractRelmDatabaseName(Attribute dbAttr)
        {
            // exact property you confirmed:
            var prop = dbAttr.GetType().GetProperty(nameof(RelmDatabase.DatabaseName), BindingFlags.Public | BindingFlags.Instance);
            return prop?.PropertyType == typeof(string) ? (string?)prop.GetValue(dbAttr) : null;
        }

        private static string? ExtractRelmTableName(Attribute tableAttr)
        {
            // exact property you confirmed:
            var prop = tableAttr.GetType().GetProperty(nameof(RelmTable.TableName), BindingFlags.Public | BindingFlags.Instance);
            return prop?.PropertyType == typeof(string) ? (string?)prop.GetValue(tableAttr) : null;
        }
    }
}
