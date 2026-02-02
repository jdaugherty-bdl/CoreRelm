using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.Introspection;
using CoreRelm.Models.Migrations.MigrationPlans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmMigrationPlanner
    {
        MigrationPlan Plan(SchemaSnapshot desired, SchemaSnapshot actual, MigrationPlanOptions options);
    }
}
