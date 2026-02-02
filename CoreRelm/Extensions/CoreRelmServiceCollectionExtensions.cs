using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.RelmInternal.Helpers.Metadata;
using CoreRelm.RelmInternal.Helpers.Migrations.Introspection;
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
        public static IServiceCollection AddCoreRelm(this IServiceCollection services, IConfiguration configuration)
        {
            RelmHelper.UseConfiguration(configuration);

            services.AddSingleton<IRelmMetadataReader, RelmMetadataReader>();
            services.AddSingleton<IRelmSchemaIntrospector, MySqlSchemaIntrospector>();
            /*
            services.AddSingleton<IRelmMigrationPlanner, RelmMigrationPlanner>();
            services.AddSingleton<IRelmMigrationSqlRenderer, MySqlMigrationSqlRenderer>();
            */
            return services;
        }
    }
}
