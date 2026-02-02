using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Models.Migrations.Introspection
{
    public sealed record ForeignKeySchema(
        string ConstraintName,
        string TableName,
        IReadOnlyList<string> ColumnNames,
        string ReferencedTableName,
        IReadOnlyList<string> ReferencedColumnNames,
        string UpdateRule,
        string DeleteRule
    );
}
