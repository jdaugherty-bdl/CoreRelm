using CoreRelm.Interfaces.Resolvers;
using CoreRelm.RelmInternal.Resolvers;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CoreRelm.RelmInternal.Helpers.Operations
{
    internal class ConnectionHelper
    {
        // a pointer to the application's resolver instance
        internal static IRelmResolver_MySQL? DALResolver = GetResolverInstance();

        /// <summary>
        /// find an object inheriting from IDALResolver, but only look in the entry assembly (where all your custom code is)
        /// once it is found, then that object is loaded through Reflection to be used later on.
        /// </summary>
        /// <returns>The application's DALResolver instance.</returns>
        internal static IRelmResolver_MySQL? GetResolverInstance()
        {
            // try to get the resolver the standard way
            /*
            var entryAssembly = AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => !string.IsNullOrWhiteSpace(x.EntryPoint?.Name))
                .SelectMany(x => x
                    .GetModules()
                    .SelectMany(y => y
                        .GetTypes()
                        .Where(z => z
                            .GetInterfaces()
                            .Any(a => a == typeof(IRelmResolver_MySQL)))))
                .FirstOrDefault();
            Console.WriteLine($"entryAssembly = {string.Join(",", AppDomain.CurrentDomain.GetAssemblies().Where(x => !string.IsNullOrWhiteSpace(x.EntryPoint?.Name)).Select(x => x?.FullName))}");
            */
            Type? entryAssembly = null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (assembly.IsDynamic)
                    continue;

                if (string.IsNullOrWhiteSpace(assembly.EntryPoint?.Name))
                    continue;

                var modules = assembly.GetModules();
                foreach (var module in modules)
                {
                    var types = module.GetTypes();
                    foreach (var type in types)
                    {
                        var interfaces = type.GetInterfaces();
                        foreach (var resolverInterface in interfaces)
                        {
                            if (resolverInterface == typeof(IRelmResolver_MySQL))
                            {
                                entryAssembly = type;
                                break;
                            }
                        }

                        if (entryAssembly != null)
                            break;
                    }

                    if (entryAssembly != null)
                        break;
                }

                if (entryAssembly != null)
                    break;
            }

            // if the standard way didn't work, do a little detective work (may not work 100% of the time)
            /*
            var clientDalResolverType =
                entryAssembly
                ??
                AppDomain
                .CurrentDomain
                .GetAssemblies()
                .Where(x => x
                    .GetCustomAttributes(true)
                    .Any(y => y is AssemblyCompanyAttribute attribute
                        && (attribute.Company.StartsWith("BV", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau Veritas", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau-Veritas", StringComparison.InvariantCultureIgnoreCase))))
                .SelectMany(x => x
                    .GetModules()
                    .SelectMany(y => y?
                        .GetTypes()?
                        .Where(z => z?
                            .GetInterfaces()?
                            .Any(a => a == typeof(IRelmResolver_MySQL))
                            ??
                            false)))
                .FirstOrDefault();
            Console.WriteLine($"clientDalResolverType");
            */
            var clientDalResolverType = entryAssembly;
            foreach (var assembly in assemblies)
            {
                var customAttributes = assembly.GetCustomAttributes(true);
                var hasAttributes = customAttributes.Any(y => y is AssemblyCompanyAttribute attribute
                        && (attribute.Company.StartsWith("BV", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau Veritas", StringComparison.InvariantCultureIgnoreCase)
                            || attribute.Company.StartsWith("Bureau-Veritas", StringComparison.InvariantCultureIgnoreCase)));

                if (!hasAttributes)
                    continue;

                var modules = assembly.GetModules();
                foreach (var module in modules)
                {
                    try
                    {
                        var types = module.GetTypes();
                        foreach (var type in types)
                        {
                            var interfaces = type.GetInterfaces();
                            foreach (var resolverInterface in interfaces)
                            {
                                if (resolverInterface == typeof(IRelmResolver_MySQL))
                                {
                                    clientDalResolverType = type;
                                    break;
                                }
                            }

                            if (clientDalResolverType != null)
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting types from module: {ex.Message}\n{ex.StackTrace}");

                        if (ex is ReflectionTypeLoadException)
                        {
                            var typeLoadException = ex as ReflectionTypeLoadException;
                            var loaderExceptions = typeLoadException.LoaderExceptions;
                            foreach (var loaderException in loaderExceptions)
                                Console.WriteLine($"Loader exception: {loaderException.Message}\n{loaderException.StackTrace}");
                        }

                        throw ex;
                    }

                    if (clientDalResolverType != null)
                        break;
                }

                if (clientDalResolverType != null)
                    break;
            }
            
            // if a resolver is found use that, otherwise use the simple default resolver
            if (clientDalResolverType != null)
                return (IRelmResolver_MySQL)Activator.CreateInstance(clientDalResolverType);
            else
                return new DefaultRelmResolver();
        }

        /// <summary>
        /// Gets a MySQL connection builder that is then used to establish a connection to the database
        /// </summary>
        /// <param name="connectionType">A properly formatted database connection string</param>
        /// <returns>A connection string builder that can be used to establish connections</returns>
        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromType(Enum connectionType)
        {
            return DALResolver?.GetConnectionBuilderFromType(connectionType);
        }

        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromName(string connectionName)
        {
            return DALResolver?.GetConnectionBuilderFromName(connectionName);
        }

        internal static MySqlConnectionStringBuilder GetConnectionBuilderFromConnectionString(string connectionString)
        {
            return DALResolver?.GetConnectionBuilderFromConnectionString(connectionString);
        }

        internal static MySqlConnection GetConnectionFromName(string connectionName, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            var connectionBuilder = GetConnectionBuilderFromName(connectionName);

            return GetConnection(connectionBuilder, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        internal static MySqlConnection GetConnectionFromType(Enum connectionType, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            var connectionBuilder = GetConnectionBuilderFromType(connectionType);

            return GetConnection(connectionBuilder, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        internal static MySqlConnection GetConnectionFromConnectionString(string? connectionString, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString), "A valid connection string must be provided.");

            var connectionBuilder = GetConnectionBuilderFromConnectionString(connectionString);

            return GetConnection(connectionBuilder, allowUserVariables: allowUserVariables, convertZeroDateTime: convertZeroDateTime, lockWaitTimeoutSeconds: lockWaitTimeoutSeconds);
        }

        private static MySqlConnection GetConnection(MySqlConnectionStringBuilder connectionBuilder, bool allowUserVariables = false, bool convertZeroDateTime = false, int lockWaitTimeoutSeconds = 0)
        {
            if (convertZeroDateTime)
                connectionBuilder.ConvertZeroDateTime = true;

            if (allowUserVariables)
                connectionBuilder.AllowUserVariables = true;

            if (lockWaitTimeoutSeconds > 0)
                connectionBuilder.DefaultCommandTimeout = (uint)lockWaitTimeoutSeconds;

            return new MySqlConnection(connectionBuilder.ToString());
        }
    }
}
