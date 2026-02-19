using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Tooling.Apply;
using CoreRelm.Models.Migrations.Tooling.Drift;
using CoreRelm.Models.Migrations.Tooling.Generation;
using CoreRelm.Models.Migrations.Tooling.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations.Tooling
{
    public interface IRelmMigrationTooling
    {
        // Model set operations (content only; host provides JSON + assembly)
        ModelSetValidationReport ValidateModelSet(ModelSetsFile modelSets, string setName, Assembly modelsAssembly, ModelSetValidateOptions? options = null);

        // Generate migration scripts (per database) from a resolved model set
        Task<MigrationGenerationResult> GenerateMigrationsAsync(ModelSetsFile modelSets, string setName, Assembly modelsAssembly, GenerateMigrationsOptions options);

        // Apply migrations (either from generated scripts in-memory, or from host-provided scripts)
        ApplyMigrationsResult ApplyMigrations(ApplyMigrationsRequest request);

        // Status/drift (host provides migration list or scripts)
        Task<MigrationStatusResult> GetStatusAsync(MigrationStatusRequest request);
    }
}
