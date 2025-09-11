using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CoreRelm.RelmEnumHolder;

namespace CoreRelm.Options
{
    public class RelmContextOptionsBuilder
    {
        public enum OptionsBuilderTypes
        {
            ConnectionString,
            NamedConnectionString,
            OpenConnection
        }

        public string DatabaseServer { get; private set; }
        public string DatabaseName { get; private set; }
        public string DatabaseUser { get; private set; }
        public string DatabasePassword { get; private set; }
        public string DatabaseConnectionString { get; private set; }
        public string NamedConnection { get; set; }

        public MySqlConnection DatabaseConnection { get; private set; }
        public MySqlTransaction DatabaseTransaction { get; private set; }

        private OptionsBuilderTypes _optionsBuilderType;
        public OptionsBuilderTypes OptionsBuilderType => _optionsBuilderType;

        private Enum _connectionStringType;
        public Enum ConnectionStringType => _connectionStringType;

        internal bool CanOpenConnection { get; set; } = true;

        public RelmContextOptionsBuilder() { }

        public RelmContextOptionsBuilder(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException("Connection string cannot be null or empty.", nameof(connectionString));

            ParseConnectionDetails(connectionString);
        }

        public RelmContextOptionsBuilder(string databaseServer, string databaseName, string databaseUser, string databasePassword)
        {
            SetDatabaseServer(databaseServer);
            SetDatabaseName(databaseName);
            SetDatabaseUser(databaseUser);
            SetDatabasePassword(databasePassword);
        }

        public RelmContextOptionsBuilder(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);
        }

        public RelmContextOptionsBuilder(MySqlConnection connection)
        {
            SetDatabaseConnection(connection);
        }

        public RelmContextOptionsBuilder(MySqlConnection connection, MySqlTransaction transaction)
        {
            SetDatabaseConnection(connection);
            SetDatabaseTransaction(transaction);
        }

        public void SetDatabaseConnection(MySqlConnection connection)
        {
            DatabaseConnection = connection ?? throw new ArgumentNullException("Connection cannot be null.", nameof(connection));

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        public void SetDatabaseTransaction(MySqlTransaction transaction)
        {
            DatabaseTransaction = transaction; // ?? throw new ArgumentNullException("Transaction cannot be null.", nameof(transaction));

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        public void SetDatabaseServer(string databaseServer)
        {
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentNullException("Database server cannot be null or empty.", nameof(databaseServer));

            this.DatabaseServer = databaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException("Database name cannot be null or empty.", nameof(databaseName));

            string pattern = @"^[a-zA-Z0-9$_\u0080-\uFFFF]+$";

            if (!Regex.IsMatch(databaseName, pattern))
                throw new ArgumentException("DatabaseName", "Invalid database name. Must be alphanumeric with underscores.");

            this.DatabaseName = databaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseUser(string databaseUser)
        {
            if (string.IsNullOrEmpty(databaseUser))
                throw new ArgumentNullException("Database user cannot be null or empty.", nameof(databaseUser));

            this.DatabaseUser = databaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabasePassword(string databasePassword)
        {
            if (string.IsNullOrEmpty(databasePassword))
                throw new ArgumentNullException("Database password cannot be null or empty.", nameof(databasePassword));

            this.DatabasePassword = databasePassword;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetConnectionStringType(Enum connectionStringType)
        {
            SetConnectionStringType(connectionStringType.GetType(), connectionStringType);
        }

        public void SetConnectionStringType(Type enumType, Enum connectionStringType)
        {
            if (!Enum.IsDefined(enumType, connectionStringType))
                throw new ArgumentNullException("Invalid connection string type provided.", nameof(connectionStringType));

            _connectionStringType = connectionStringType;

            NamedConnection = connectionStringType.ToString();

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        public void SetNamedConnection(string namedConnection)
        {
            if (string.IsNullOrEmpty(namedConnection))
                throw new ArgumentNullException(nameof(namedConnection));

            NamedConnection = namedConnection;

            /*
            if (!Enum.TryParse(DatabaseConnectionString, out _connectionStringType))
                throw new ArgumentException($"Invalid connection string type '{DatabaseConnectionString}'.");
            ConnectionStringType = (DALHelper.ConnectionStringTypes)Enum.Parse(typeof(DALHelper.ConnectionStringTypes), DatabaseConnectionString);
            */

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        public void SetDatabaseConnectionString(string DatabaseConnectionString)
        {
            if (string.IsNullOrEmpty(DatabaseConnectionString))
                throw new ArgumentNullException(nameof(DatabaseConnectionString));

            this.DatabaseConnectionString = DatabaseConnectionString;
        }

        public bool ValidateAllSettings(bool throwExceptions = true)
        {
            if (_optionsBuilderType == OptionsBuilderTypes.NamedConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseConnectionString))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseConnectionString", "DatabaseConnectionString cannot be null or empty when using a named connection string.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.OpenConnection)
            {
                if (DatabaseConnection == null)
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseConnection), "Database connection cannot be null.");
                    else
                        return false;
                }

                return true;
            }
            else if (_optionsBuilderType == OptionsBuilderTypes.ConnectionString)
            {
                if (string.IsNullOrEmpty(DatabaseServer))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseServer", "Database Server cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseName))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseName", "Database Name cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseUser))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabaseUser", "Username cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabasePassword))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException("DatabasePassword", "Password cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                return true;
            }
            else
            {
                throw new ArgumentException($"Invalid connection string type '{ConnectionStringType}'.");
            }
        }

        public void ParseConnectionDetails(string connectionDetails)
        {
            if (string.IsNullOrWhiteSpace(connectionDetails))
                throw new ArgumentNullException(nameof(connectionDetails));

            if (!connectionDetails.Contains("="))
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");

            var connectionOptions = connectionDetails
                .Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                .ToDictionary(x => x[0].ToLower(), x => x[1]);

            // Check for either a 'name' key or all four individual connection parameters.
            if (!(connectionOptions.ContainsKey("name") ||
                  (connectionOptions.ContainsKey("server") &&
                   connectionOptions.ContainsKey("database") &&
                   (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) &&
                   (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password")))))
            {
                throw new ArgumentException("Incomplete connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if ((connectionOptions.ContainsKey("name") &&
                    connectionOptions.Keys.Count > 1) ||
                (connectionOptions.ContainsKey("server") &&
                    connectionOptions.ContainsKey("database") &&
                   (connectionOptions.ContainsKey("uid") || connectionOptions.ContainsKey("user") || connectionOptions.ContainsKey("user id")) &&
                   (connectionOptions.ContainsKey("pwd") || connectionOptions.ContainsKey("password")) &&
                    connectionOptions.Keys.Count > 4))
            {
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");
            }

            if (connectionOptions.ContainsKey("name"))
            {
                SetNamedConnection(connectionOptions["name"]);

                var connectionBuilder = RelmHelper.GetConnectionBuilderFromName(connectionOptions["name"]);
                var connectionString = connectionBuilder?.ConnectionString;

                SetDatabaseConnectionString(connectionString);
            }
            else
            {
                if (connectionOptions.ContainsKey("server"))
                    SetDatabaseServer(connectionOptions["server"]);

                if (connectionOptions.ContainsKey("database"))
                    SetDatabaseName(connectionOptions["database"]);

                if (connectionOptions.ContainsKey("uid"))
                    SetDatabaseUser(connectionOptions["uid"]);
                else if (connectionOptions.ContainsKey("user"))
                    SetDatabaseUser(connectionOptions["user"]);
                else if (connectionOptions.ContainsKey("user id"))
                    SetDatabaseUser(connectionOptions["user id"]);

                if (connectionOptions.ContainsKey("pwd"))
                    SetDatabasePassword(connectionOptions["pwd"]);
                else if (connectionOptions.ContainsKey("password"))
                    SetDatabasePassword(connectionOptions["password"]);

                DatabaseConnectionString = $"server={DatabaseServer};database={DatabaseName};user id={DatabaseUser};password={DatabasePassword}";
            }
        }
    }
}
