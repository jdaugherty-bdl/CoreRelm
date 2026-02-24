using CoreRelm.Interfaces.Metadata;
using CoreRelm.Interfaces.Migrations;
using CoreRelm.Models.Migrations;
using CoreRelm.RelmInternal.Helpers.Migrations.Providers;
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
        IRelmDatabaseProvisioner _provisioner,
        IRelmSchemaIntrospector _introspector,
        IRelmMigrationPlanner _planner,
        IRelmMigrationSqlRenderer _renderer,
        IRelmDesiredSchemaBuilder _desiredBuilder,
        ILogger<IMigrationSqlProvider>? _log = null) : IRelmMigrationSqlProviderFactory
    {
        public IMigrationSqlProvider CreateProvider(MigrationOptions migrationOptions)
        {
            return new DefaultRelmMigrationSqlProvider(
                _introspector,
                _planner,
                _renderer,
                _provisioner,
                _desiredBuilder,
                migrationOptions,
                _log);
        }
    }
}
