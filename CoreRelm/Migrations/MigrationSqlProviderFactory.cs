using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
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
        private readonly IConfiguration _cfg;

        public MigrationSqlProviderFactory(
            IRelmSchemaIntrospector introspector,
            IRelmMigrationPlanner planner,
            IRelmMigrationSqlRenderer renderer,
            IConfiguration cfg)
        {
            _introspector = introspector;
            _planner = planner;
            _renderer = renderer;
            _cfg = cfg;
        }

        public IMigrationSqlProvider CreateProvider(bool quiet)
        {
            var serverConn = _cfg["MySql:ServerConnectionString"] ?? "";
            var dbTemplate = _cfg["MySql:DatabaseConnectionStringTemplate"] ?? "";

            return new DefaultRelmMigrationSqlProvider(
                _introspector,
                _planner,
                _renderer,
                new MySqlDatabaseProvisioner(),
                serverConn,
                dbTemplate,
                quiet);
        }
    }
}
