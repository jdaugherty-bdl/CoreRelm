using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Interfaces.Migrations.MigrationFiles;
using CoreRelm.Interfaces.Migrations.Tooling;
using CoreRelm.Interfaces.ModelSets;
using CoreRelm.Migrations;
using CoreRelm.Migrations.MigrationFiles;
using CoreRelm.Options;
using CoreRelm.RelmInternal.Helpers.Metadata;
using CoreRelm.RelmInternal.Helpers.Migrations.Execution;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
using CoreRelm.RelmInternal.Helpers.Migrations.MigrationPlans;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using CoreRelm.RelmInternal.Helpers.Migrations.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class CoreRelmServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreRelm(this IServiceCollection services, IConfiguration configuration)
        {
            RelmHelper.UseConfiguration(configuration);

            AddCore(services);
            AddMigrations(services);        // split later
            AddToolingParsers(services);    // split later

            return services;
        }

        public static IServiceCollection AddRelmContext<T>(
            this IServiceCollection services,
            Action<RelmContextOptionsBuilder> configure)
            where T : class
        {
            var builder = new RelmContextOptionsBuilder();
            configure(builder);

            var options = builder.BuildOptions();
            services.AddSingleton(options);
            services.AddScoped<T>(sp => ActivatorUtilities.CreateInstance<T>(sp, options));
            
            return services;
        }

        private static void AddCore(IServiceCollection services)
        {
            services.AddSingleton<IRelmMetadataReader, RelmMetadataReader>();
            // other core ORM services...
        }

        private static void AddMigrations(IServiceCollection services)
        {
            services.AddSingleton<IRelmSchemaIntrospector, MySqlSchemaIntrospector>();
            services.AddSingleton<IRelmMigrationPlanner, RelmMigrationPlanner>();
            services.AddSingleton<IRelmMigrationSqlRenderer, MySqlMigrationSqlRenderer>();

            services.AddSingleton<IRelmDatabaseProvisioner, MySqlDatabaseProvisioner>();
            services.AddSingleton<IRelmSqlScriptRunner, MySqlScriptRunner>();
            services.AddSingleton<IRelmSchemaMigrationsStore, SchemaMigrationsStore>();
            services.AddSingleton<IRelmDesiredSchemaBuilder, DesiredSchemaBuilder>();
        }

        private static void AddToolingParsers(IServiceCollection services)
        {
            services.AddSingleton<IRelmMigrationTooling, RelmMigrationTooling>();
            services.AddSingleton<IMigrationFileNameParser, MigrationFileNameParser>();
            services.AddSingleton<IModelSetParser, ModelSetParser>();
            services.AddSingleton<IModelSetResolver, ModelSetResolver>();

            var versionString = typeof(MigrationScriptHeaderParser).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "MaxSupportedMigrationFileVersion")?.Value;

            var maxVersion = Version.TryParse(versionString, out var v) ? v : new Version(1, 0, 0);
            services.AddKeyedSingleton("MaxSupportedMigrationFileVersion", maxVersion);

            services.AddTransient<MigrationScriptHeaderParser>();
        }
    }
}
