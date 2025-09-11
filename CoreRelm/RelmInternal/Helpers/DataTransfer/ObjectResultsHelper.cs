using CoreRelm.Interfaces;
using CoreRelm.Interfaces.RelmQuick;
using CoreRelm.Models;
using CoreRelm.RelmInternal.Helpers.Operations;
using CoreRelm.RelmInternal.Helpers.Utilities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.DataTransfer
{
    internal class ObjectResultsHelper
    {
        internal static IEnumerable<T> GetDataList<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false)
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetDataList<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
            }
        }

        internal static IEnumerable<T> GetDataList<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null)
        {
            var relmContext = new RelmContext(ExistingConnection, SqlTransaction);

            return GetDataList<T>(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException);
        }

        internal static IEnumerable<T> GetDataList<T>(IRelmContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true)
        {
            return RefinedResultsHelper.GetDataTable(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException)
                .AsEnumerable()
                .Select(x => (T)CoreUtilities.ConvertScalar<T>(x[0]));
        }

        internal static IEnumerable<T> GetDataList<T>(IRelmQuickContext relmContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true)
        {
            return RefinedResultsHelper.GetDataTable(relmContext, QueryString, Parameters: Parameters, ThrowException: ThrowException)
                .AsEnumerable()
                .Select(x => (T)CoreUtilities.ConvertScalar<T>(x[0]));
        }

        internal static T GetDataObject<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetDataObject<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
            }
        }

        internal static T GetDataObject<T>(IRelmContext CurrentContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel
        {
            return GetDataObject<T>(CurrentContext.ContextOptions.DatabaseConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: CurrentContext.ContextOptions.DatabaseTransaction);
        }

        internal static T GetDataObject<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel
        {
            return GetDataObjects<T>(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction)
                .FirstOrDefault();
        }

        internal static IEnumerable<T> GetDataObjects<T>(Enum ConfigConnectionString, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel
        {
            using (var conn = ConnectionHelper.GetConnectionFromType(ConfigConnectionString, AllowUserVariables))
            {
                return GetDataObjects<T>(conn, QueryString, Parameters: Parameters, ThrowException: ThrowException);
            }
        }

        internal static IEnumerable<T> GetDataObjects<T>(MySqlConnection ExistingConnection, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, MySqlTransaction SqlTransaction = null) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(ExistingConnection, QueryString, Parameters: Parameters, ThrowException: ThrowException, SqlTransaction: SqlTransaction));
        }

        internal static IEnumerable<T> GetDataObjects<T>(IRelmQuickContext CurrentContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(CurrentContext, QueryString, Parameters: Parameters, ThrowException: ThrowException));
        }

        internal static IEnumerable<T> GetDataObjects<T>(IRelmContext CurrentContext, string QueryString, Dictionary<string, object> Parameters = null, bool ThrowException = true, bool AllowUserVariables = false) where T : IRelmModel
        {
            return GetDataObjects<T>(RefinedResultsHelper.GetDataTable(CurrentContext, QueryString, Parameters: Parameters, ThrowException: ThrowException));
        }

        internal static IEnumerable<T> GetDataObjects<T>(DataTable existingData) where T : IRelmModel
        {
            /*
            var allConstructors = CoreUtilities.GetConstructorsRecursively(typeof(T));
            var dataRowConstructor = allConstructors.FirstOrDefault(x => x.GetParameters().Any(y => y.ParameterType == typeof(DataRow)))
                ?? throw new MethodAccessException($"No constructor receiving type DataRow found on type: [{typeof(T).FullName}]");
            */
            return existingData
                .AsEnumerable()
                .Select(x => x == null
                    ? default
                    /*
                    //: (typeof(T).GetConstructors().Any(y => y.GetParameters().Length > 2)
                    : (dataRowConstructor.GetParameters().Length == 4
                        ? CoreUtilities.CreateCreatorExpression<DataRow, string, bool, bool, T>()(x, null, false, false)
                        : CoreUtilities.CreateCreatorExpression<DataRow, string, T>()(x, null)));
                    */
                    : (T)CoreUtilities.CreateCreatorExpression<T>()().ResetWithData(x, null));
        }
    }
}
