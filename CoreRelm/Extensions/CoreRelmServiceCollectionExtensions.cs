using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
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
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Extensions
{
    public static class CoreRelmServiceCollectionExtensions
    {
        // TODO: split up registration into multiple methods?
        /*
        AddCoreRelmCore() – metadata + db primitives
        AddCoreRelmMySql() – MySQL-specific impls
        AddCoreRelmTooling() – optional CLI-ish helpers (modelsets/migration files) if you insist
        AddCoreRelmMigrations() – full migrations pipeline (executor, applier, etc.)
        */
        /*
        public static IServiceCollection AddCoreRelm(this IServiceCollection services, IConfiguration configuration)
        {
            RelmHelper.UseConfiguration(configuration);

            // Core schema & migrations
            services.AddSingleton<IRelmMetadataReader, RelmMetadataReader>();
            services.AddSingleton<IRelmSchemaIntrospector, MySqlSchemaIntrospector>();
            services.AddSingleton<IRelmMigrationPlanner, RelmMigrationPlanner>();
            services.AddSingleton<IRelmMigrationSqlRenderer, MySqlMigrationSqlRenderer>();

            // Provisioning (database existence)
            services.AddSingleton<IRelmDatabaseProvisioner, MySqlDatabaseProvisioner>();

            // Script execution + migration tracking
            services.AddSingleton<IRelmSqlScriptRunner, MySqlScriptRunner>();
            services.AddSingleton<IRelmSchemaMigrationsStore, SchemaMigrationsStore>();

            // Desired schema builder (from CLR models / descriptors)
            services.AddSingleton<IRelmDesiredSchemaBuilder, DesiredSchemaBuilder>();

            // Optional: “apply pipeline” executor if you created it
            //services.AddSingleton<IRelmMigrationExecutor, RelmMigrationExecutor>();

            return services;
        }
        */
        public static IServiceCollection AddCoreRelm(this IServiceCollection services, IConfiguration configuration)
        {
            RelmHelper.UseConfiguration(configuration);

            AddCore(services);
            AddMigrations(services);        // split later
            AddToolingParsers(services);    // split later

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
            /*
            services.AddSingleton<IModelSetParser, ModelSetParser>();
            services.AddSingleton<IMigrationFileNameParser, MigrationFileNameParser>();
            */
        }
    }
}
