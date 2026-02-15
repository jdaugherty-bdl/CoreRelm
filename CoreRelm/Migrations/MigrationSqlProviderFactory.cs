using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{
    public sealed class MigrationSqlProviderFactory(
        IServiceProvider serviceProvider,
        IRelmSchemaIntrospector introspector,
        IRelmMigrationPlanner planner,
        IRelmMigrationSqlRenderer renderer,
        IRelmDesiredSchemaBuilder desiredBuilder,
        ILogger<MySqlDatabaseProvisioner>? log = null) : IRelmMigrationSqlProviderFactory
    {
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IRelmSchemaIntrospector _introspector = introspector;
        private readonly IRelmMigrationPlanner _planner = planner;
        private readonly IRelmMigrationSqlRenderer _renderer = renderer;
        private readonly IRelmDesiredSchemaBuilder _desiredBuilder = desiredBuilder;
        private readonly ILogger<MySqlDatabaseProvisioner>? _log = log;

        public IMigrationSqlProvider CreateProvider(MigrationOptions migrationOptions)
        {
            return new DefaultRelmMigrationSqlProvider(
                _serviceProvider,
                _introspector,
                _planner,
                _renderer,
                new MySqlDatabaseProvisioner(),
                _desiredBuilder,
                migrationOptions,
                _log);
        }
    }
}
