using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.Interfaces
{
    public interface IRelmModel
    {
        long? Id { get; set; }
        bool Active { get; set; }
        string InternalId { get; set; }
        DateTime CreateDate { get; set; }
        DateTime LastUpdated { get; set; }
        IRelmModel ResetCoreAttributes(bool NullInternalId = false);
        IRelmModel ResetWithData(DataRow ModelData, string? AlternateTableName = null);
        List<KeyValuePair<string, Tuple<string, PropertyInfo>>> GetUnderscoreProperties(bool GetOnlyDbResolvables = true);
        int WriteToDatabase(Enum ConnectionStringType, int BatchSize = 10);
        int WriteToDatabase(DbConnection ExistingConnection, DbTransaction? SqlTransaction = null, int BatchSize = 10);
        dynamic GenerateDTO(IEnumerable<string>? IncludeProperties = null, IEnumerable<string>? ExcludeProperties = null, string? SourceObjectName = null, Func<IRelmModel, Dictionary<string, object>>? GetAdditionalObjectProperties = null, int Iteration = 0);
    }
}
