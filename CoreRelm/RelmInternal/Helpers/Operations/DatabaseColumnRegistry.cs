using MoreLinq;
using MySql.Data.MySqlClient;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreRelm.Interfaces;
using CoreRelm.Models;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class DatabaseColumnRegistry<T>
    {
        private readonly string? databaseName;
        private readonly IRelmContext? context;

        private Dictionary<string, DALTableRowDescriptor>? tableRowDescriptors;
        private Dictionary<string, string>? underscoreProperties;

        public Dictionary<string, Tuple<string, DALTableRowDescriptor?>>? PropertyColumns { get; private set; }
        public Dictionary<string, Tuple<string, DALTableRowDescriptor?>>? DatabaseColumns => PropertyColumns?.Where(x => x.Value.Item2 != null).ToDictionary(x => x.Key, x => x.Value);
        public bool HasDatabaseColumns => (DatabaseColumns?.Count ?? 0) > 0;

        public DatabaseColumnRegistry()
        {
            context = null;
            databaseName = null;
            tableRowDescriptors = null;
            underscoreProperties = null;

            SetupPropertyLists();
        }
        /*
        public DatabaseColumnRegistry(MySqlConnection? connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            context = new RelmContext(connection);

            databaseName = context.ContextOptions?.DatabaseConnection?.Database;

            SetupPropertyLists();
        }

        public DatabaseColumnRegistry(Enum? connectionStringType)
        {
            ArgumentNullException.ThrowIfNull(connectionStringType);

            context = new RelmContext(connectionStringType);

            databaseName = context.ContextOptions?.DatabaseConnection?.Database;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("The provided connection string type does not have a valid database name.");
            }

            SetupPropertyLists();
        }
        */

        public DatabaseColumnRegistry(IRelmContext? context)
        {
            ArgumentNullException.ThrowIfNull(context);

            this.context = context;

            databaseName = this.context.ContextOptions?.DatabaseConnection?.Database;

            SetupPropertyLists();
        }

        private void SetupPropertyLists()
        { 
            // get a list of all properties on T that are marked with the DALResolvable attribute
            underscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            PropertyColumns = underscoreProperties.ToDictionary(x => x.Key, x => new Tuple<string, DALTableRowDescriptor?>(x.Value, null));
        }

        public Dictionary<string, DALTableRowDescriptor>? ReadDatabaseDescriptions(string tableName)
        {
            if (context == null)
                throw new InvalidOperationException("Cannot read database descriptions without a valid IRelmContext.");

            if (context.ContextOptions?.DatabaseConnection == null)
                throw new InvalidOperationException("Cannot read database descriptions without valid database connection information in the IRelmContext.");

            if (context.ContextOptions.DatabaseConnection.State != System.Data.ConnectionState.Open)
                throw new InvalidOperationException("Cannot read database descriptions because the database connection in the IRelmContext is not open.");

            // pull the table details from the database
            var descriptorRows = RelmHelper.GetDataObjects<DALTableRowDescriptor>(context, $"DESCRIBE {(string.IsNullOrWhiteSpace(databaseName) ? string.Empty : $"{databaseName}.")}{tableName}")
                ?.ToDictionary(x => x!.Field, x => x);

            if (descriptorRows == null)
                return default;

            tableRowDescriptors = new Dictionary<string, DALTableRowDescriptor>(descriptorRows!, StringComparer.OrdinalIgnoreCase);

            tableRowDescriptors
                .ForEach(x =>
                {
                    x.Value.IsAutoIncrement = x.Value.Extra.Contains("auto_increment");
                    x.Value.IsPrimaryKey = x.Value.Key.Contains("PRI");
                    x.Value.IsUniqueConstraint = x.Value.Key.Contains("UNI");
                });

            PropertyColumns = underscoreProperties
                ?.ToDictionary(x => x.Key, x => new Tuple<string, DALTableRowDescriptor?>(x.Value, tableRowDescriptors.ContainsKey(x.Value) ? tableRowDescriptors[x.Value] : null));

            return tableRowDescriptors;
        }
    }
}
