using MoreLinq;
using MySql.Data.MySqlClient;
using CoreRelm.RelmInternal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class DatabaseColumnRegistry<T>
    {
        private readonly string? databaseName;
        private readonly MySqlConnection? connection;
        private readonly Enum? connectionStringType;

        private Dictionary<string, DALTableRowDescriptor>? tableRowDescriptors;
        private Dictionary<string, string>? underscoreProperties;

        public Dictionary<string, Tuple<string, DALTableRowDescriptor?>>? PropertyColumns { get; private set; }
        public Dictionary<string, Tuple<string, DALTableRowDescriptor?>>? DatabaseColumns => PropertyColumns?.Where(x => x.Value.Item2 != null).ToDictionary(x => x.Key, x => x.Value);
        public bool HasDatabaseColumns => (DatabaseColumns?.Count ?? 0) > 0;

        public DatabaseColumnRegistry()
        {
            SetupPropertyLists();
        }

        public DatabaseColumnRegistry(MySqlConnection? connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            this.connection = connection;

            databaseName = this.connection.Database;

            SetupPropertyLists();
        }

        public DatabaseColumnRegistry(Enum? connectionStringType)
        {
            ArgumentNullException.ThrowIfNull(connectionStringType);

            this.connectionStringType = connectionStringType;

            databaseName = RelmHelper.GetConnectionBuilderFromConnectionType(connectionStringType)?.Database;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentException("The provided connection string type does not have a valid database name.");
            }

            SetupPropertyLists();
        }

        private void SetupPropertyLists()
        { 
            // get a list of all properties on T that are marked with the DALResolvable attribute
            underscoreProperties = DataNamingHelper.GetUnderscoreProperties<T>(true).ToDictionary(x => x.Value.Item1, x => x.Key);

            PropertyColumns = underscoreProperties.ToDictionary(x => x.Key, x => new Tuple<string, DALTableRowDescriptor?>(x.Value, null));
        }

        public Dictionary<string, DALTableRowDescriptor> ReadDatabaseDescriptions(string tableName)
        {
            // pull the table details from the database
            var tableRowDescriptors1 = ((connection != null)
                    ? RelmHelper.GetDataObjects<DALTableRowDescriptor>(connection, $"DESCRIBE {(string.IsNullOrWhiteSpace(databaseName) ? string.Empty : $"{databaseName}.")}{tableName}")
                    : RelmHelper.GetDataObjects<DALTableRowDescriptor>(connectionStringType, $"DESCRIBE {(string.IsNullOrWhiteSpace(databaseName) ? string.Empty : $"{databaseName}.")}{tableName}"))
                ?.ToDictionary(x => x.Field, x => x);

            tableRowDescriptors = new Dictionary<string, DALTableRowDescriptor>(tableRowDescriptors1, StringComparer.OrdinalIgnoreCase);

            tableRowDescriptors
                .ForEach(x =>
                {
                    x.Value.IsAutoIncrement = x.Value.Extra.Contains("auto_increment");
                    x.Value.IsPrimaryKey = x.Value.Key.Contains("PRI");
                    x.Value.IsUniqueConstraint = x.Value.Key.Contains("UNI");
                });

            PropertyColumns = underscoreProperties
                .ToDictionary(x => x.Key, x => new Tuple<string, DALTableRowDescriptor?>(x.Value, tableRowDescriptors.ContainsKey(x.Value) ? tableRowDescriptors[x.Value] : null));

            return tableRowDescriptors;
        }
    }
}
