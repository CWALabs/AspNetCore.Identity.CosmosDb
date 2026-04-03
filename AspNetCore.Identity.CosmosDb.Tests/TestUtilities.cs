using AspNetCore.Identity.CosmosDb.Containers;
using AspNetCore.Identity.CosmosDb.Repositories;
using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    public class TestUtilities
    {
        /// <summary>
        /// Non-normalized email address for user 1
        /// </summary>
        public const string IDENUSER1EMAIL = "Foo1@acme.com";

        /// <summary>
        /// Non-normalized email address for user 2
        /// </summary>
        public const string IDENUSER2EMAIL = "Foo2@acme.com";


        public const string IDENUSER1ID = "507b7565-493e-49d7-94c7-d60e21036b4a";

        public const string IDENUSER2ID = "55250c6f-7c91-465a-a9ce-ea9bbe6caf81";

        /// <summary>
        /// Gets the configuration
        /// </summary>
        /// <returns></returns>
        public static IConfigurationRoot GetConfig()
        {
            // the type specified here is just so the secrets library can 
            // find the UserSecretId we added in the csproj file
            var jsonConfig = Path.Combine(Environment.CurrentDirectory, "appsettings.json");

            var builder = new ConfigurationBuilder()
                .AddJsonFile(jsonConfig, true)
                .AddEnvironmentVariables() // Added to read environment variables from GitHub Actions
                .AddUserSecrets(Assembly.GetExecutingAssembly(), true); // User secrets override all - put here

            return Retry.Do(() => builder.Build(), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Gets the value of a configuration key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetKeyValue(string key)
        {
            return GetKeyValue(GetConfig(), key);
        }

        private static string GetKeyValue(IConfigurationRoot config, string key)
        {
            var data = config[key];

            if (string.IsNullOrEmpty(data))
            {
                // First attempt to get the value of the key as named.
                data = Environment.GetEnvironmentVariable(key);

                if (string.IsNullOrEmpty(data))
                {
                    // For Github Actions, secrets are forced upper case
                    data = Environment.GetEnvironmentVariable(key.ToUpper());
                }

                // Connection string maybe?
                if (string.IsNullOrEmpty(data))
                {
                    data = config.GetConnectionString(key);
                }


                // Connection all caps string maybe?
                if (string.IsNullOrEmpty(data))
                {
                    data = config.GetConnectionString(key.ToUpper());
                }
            }

            return string.IsNullOrEmpty(data) ? string.Empty : data;
        }

        /// <summary>
        /// Get Cosmos DB Options
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="dbName"></param>
        /// <returns></returns>
        public DbContextOptions GetDbOptions(string connectionString, string databaseName)
        {
            var builder = new DbContextOptionsBuilder();
            builder.UseCosmos(connectionString, databaseName);

            return builder.Options;
        }

        /// <summary>
        /// Gets an instance of the container utilities
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public ContainerUtilities GetContainerUtilities(string connectionString, string databaseName)
        {
            var utilities = new ContainerUtilities(connectionString, databaseName);
            return utilities;
        }

        /// <summary>
        /// Ensures the test database and required containers exist.
        /// </summary>
        public async Task EnsureIdentityInfrastructureAsync(string connectionString, string databaseName)
        {
            using var utilities = GetContainerUtilities(connectionString, databaseName);
            await utilities.CreateDatabaseAsync(databaseName);
            await utilities.CreateRequiredContainers();

            using var dbContext = GetDbContext(connectionString, databaseName);
            await dbContext.Database.EnsureCreatedAsync();
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Get an instance of the Cosmos DB context.
        /// </summary>
        /// <param name="connectionName"></param>
        /// <param name="backwardCompatibility"
        /// <returns></returns>
        public CosmosIdentityDbContext<IdentityUser, IdentityRole, string> GetDbContext(
            string connectionString, string databaseName, bool backwardCompatibility = false)
        {                       
            var dbContext =
                new CosmosIdentityDbContext<IdentityUser, IdentityRole, string>(GetDbOptions(connectionString, databaseName), backwardCompatibility);
            return dbContext;
        }

        /// <summary>
        /// Get an instance of the Cosmos DB user store.
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public CosmosUserStore<IdentityUser, IdentityRole, string> GetUserStore(string connectionString, string databaseName)
        {
            var repository =
                new CosmosIdentityRepository<CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IdentityUser,
                    IdentityRole, string>(GetDbContext(connectionString, databaseName));
            var userStore = new CosmosUserStore<IdentityUser, IdentityRole, string>(repository);
            return userStore;
        }

        /// <summary>
        /// Get an instance of the Cosmos DB role store
        /// </summary>
        /// <returns></returns>
        public CosmosRoleStore<IdentityRole, string> GetRoleStore(string connectionString, string databaseName)
        {
            var repository =
                new CosmosIdentityRepository<CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IdentityUser,
                    IdentityRole, string>(GetDbContext(connectionString, databaseName));
            var rolestore = new CosmosRoleStore<IdentityRole, string>(repository);
            return rolestore;
        }

        /// <summary>
        /// Get an instance of the role manager
        /// </summary>
        /// <returns></returns>
        public RoleManager<IdentityRole> GetRoleManager(string connectionString, string databaseName)
        {
            var userStore = GetRoleStore(connectionString, databaseName);
            var userManager =
                new RoleManager<IdentityRole>(userStore, null, null, null, GetLogger<RoleManager<IdentityRole>>());
            return userManager;
        }

        /// <summary>
        /// Get a mock logger
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public ILogger<T> GetLogger<T>()
        {
            return new Logger<T>(new NullLoggerFactory());
        }
    }
}