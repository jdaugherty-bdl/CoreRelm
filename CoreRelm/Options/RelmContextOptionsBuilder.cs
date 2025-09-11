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

        public DatabaseType DatabaseType { get; private set; }

        public string? DatabaseServer { get; private set; }
        public string? DatabaseName { get; private set; }
        public string? DatabaseUser { get; private set; }
        public string? DatabasePassword { get; private set; }
        public string? DatabaseConnectionString { get; private set; }
        public string? NamedConnection { get; set; }

        public MySqlConnection DatabaseConnection { get; private set; }
        public MySqlTransaction DatabaseTransaction { get; private set; }

        private OptionsBuilderTypes? _optionsBuilderType;
        public OptionsBuilderTypes? OptionsBuilderType => _optionsBuilderType;

        private Enum? _connectionStringType;
        public Enum? ConnectionStringType => _connectionStringType;

        private static readonly char[] argumentsSeparator = [';'];
        private static readonly char[] keyValueSeparator = ['='];

        public RelmContextOptionsBuilder() { }

        public RelmContextOptionsBuilder(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "Connection string cannot be null or empty.");

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
            DatabaseConnection = connection ?? throw new ArgumentNullException(nameof(connection), "Connection cannot be null.");

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        public void SetDatabaseTransaction(MySqlTransaction transaction)
        {
            DatabaseTransaction = transaction ?? throw new ArgumentNullException(nameof(transaction), "Transaction cannot be null.");

            _optionsBuilderType = OptionsBuilderTypes.OpenConnection;
        }

        public void SetDatabaseServer(string databaseServer)
        {
            if (string.IsNullOrEmpty(databaseServer))
                throw new ArgumentNullException(nameof(databaseServer), "Database server cannot be null or empty.");

            this.DatabaseServer = databaseServer;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseName(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName), "Database name cannot be null or empty.");

            string pattern = @"^[a-zA-Z0-9$_\u0080-\uFFFF]+$";

            if (!Regex.IsMatch(databaseName, pattern))
                throw new ArgumentException("Invalid database name. Must be alphanumeric with underscores.", nameof(databaseName));

            this.DatabaseName = databaseName;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabaseUser(string databaseUser)
        {
            if (string.IsNullOrEmpty(databaseUser))
                throw new ArgumentNullException(nameof(databaseUser), "Database user cannot be null or empty.");

            this.DatabaseUser = databaseUser;

            _optionsBuilderType = OptionsBuilderTypes.ConnectionString;
        }

        public void SetDatabasePassword(string databasePassword)
        {
            if (string.IsNullOrEmpty(databasePassword))
                throw new ArgumentNullException(nameof(databasePassword), "Database password cannot be null or empty.");

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
                throw new ArgumentNullException(nameof(connectionStringType), "Invalid connection string type provided.");

            _connectionStringType = connectionStringType;

            NamedConnection = connectionStringType.ToString();

            _optionsBuilderType = OptionsBuilderTypes.NamedConnectionString;
        }

        public void SetNamedConnection(string namedConnection)
        {
            if (string.IsNullOrEmpty(namedConnection))
                throw new ArgumentNullException(nameof(namedConnection));

            NamedConnection = namedConnection;

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
                        throw new ArgumentNullException(nameof(DatabaseConnectionString), "DatabaseConnectionString cannot be null or empty when using a named connection string.");
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
                        throw new ArgumentNullException(nameof(DatabaseServer), "Database Server cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseName))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseName), "Database Name cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabaseUser))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabaseUser), "Username cannot be null or empty when using a connection string.");
                    else
                        return false;
                }

                if (string.IsNullOrEmpty(DatabasePassword))
                {
                    if (throwExceptions)
                        throw new ArgumentNullException(nameof(DatabasePassword), "Password cannot be null or empty when using a connection string.");
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

            if (!connectionDetails.Contains('='))
                throw new ArgumentException("Invalid connection details. Must be in the format of 'name=connectionString' or 'server=serverName;database=databaseName;user=userName;password=password'.");

            var connectionOptions = connectionDetails
                .Split(argumentsSeparator, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split(keyValueSeparator, StringSplitOptions.RemoveEmptyEntries))
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

            if (connectionOptions.TryGetValue("name", out string? name))
            {
                SetNamedConnection(name);

                SetDatabaseConnectionString(RelmHelper.GetConnectionBuilderFromName(name).ConnectionString);
            }
            else
            {
                if (connectionOptions.TryGetValue("server", out string? server))
                    SetDatabaseServer(server);

                if (connectionOptions.TryGetValue("database", out string? database))
                    SetDatabaseName(database);

                if (connectionOptions.TryGetValue("uid", out string? uid))
                    SetDatabaseUser(uid);
                else if (connectionOptions.TryGetValue("user", out string? user))
                    SetDatabaseUser(user);
                else if (connectionOptions.TryGetValue("user id", out string? userId))
                    SetDatabaseUser(userId);

                if (connectionOptions.TryGetValue("pwd", out string? pwd))
                    SetDatabasePassword(pwd);
                else if (connectionOptions.TryGetValue("password", out string? password))
                    SetDatabasePassword(password);

                DatabaseConnectionString = $"server={DatabaseServer};database={DatabaseName};user id={DatabaseUser};password={DatabasePassword}";
            }
        }
    }
}
