using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Provisioning;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Migrations
{

    public sealed class MigrationSqlProviderFactory : IRelmMigrationSqlProviderFactory
    {
        private readonly IRelmSchemaIntrospector _introspector;
        private readonly IRelmMigrationPlanner _planner;
        private readonly IRelmMigrationSqlRenderer _renderer;
        private readonly IRelmDesiredSchemaBuilder _desiredBuilder;

        public MigrationSqlProviderFactory(
            IRelmSchemaIntrospector introspector,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            IRelmDesiredSchemaBuilder desiredBuilder)
        {
            _introspector = introspector;
            _planner = planner;
            _renderer = renderer;
            _desiredBuilder = desiredBuilder;
        }

        public IMigrationSqlProvider CreateProvider(MigrationOptions migrationOptions)
        {
            return new DefaultRelmMigrationSqlProvider(
                _introspector,
                _planner,
                _renderer,
                new MySqlDatabaseProvisioner(),
                _desiredBuilder,
                migrationOptions);
        }
    }
}
