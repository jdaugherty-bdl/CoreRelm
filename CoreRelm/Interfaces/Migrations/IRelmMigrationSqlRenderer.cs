using CoreRelm.Models.Migrations;
using CoreRelm.Models.Migrations.MigrationPlans;
using CoreRelm.Models.Migrations.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces.Migrations
{
    public interface IRelmMigrationSqlRenderer
    {
        string Render(MigrationPlan plan, MySqlRenderOptions? options = null);
    }
}
