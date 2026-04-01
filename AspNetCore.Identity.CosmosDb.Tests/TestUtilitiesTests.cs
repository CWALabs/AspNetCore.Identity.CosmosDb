using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    [DoNotParallelize]
    public class TestUtilitiesTests
    {
        [TestMethod]
        public void GetKeyValue_ReturnsEnvironmentVariable_WhenConfigMissing()
        {
            const string key = "testutilities_env_key";
            const string value = "value-from-env";
            var original = Environment.GetEnvironmentVariable(key);

            try
            {
                Environment.SetEnvironmentVariable(key, value);
                var result = TestUtilities.GetKeyValue(key);
                Assert.AreEqual(value, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(key, original);
            }
        }

        [TestMethod]
        public void GetKeyValue_UsesUppercaseFallback()
        {
            const string key = "testutilities_upper_case_key";
            const string value = "value-from-upper";
            var original = Environment.GetEnvironmentVariable(key.ToUpperInvariant());

            try
            {
                Environment.SetEnvironmentVariable(key.ToUpperInvariant(), value);
                var result = TestUtilities.GetKeyValue(key);
                Assert.AreEqual(value, result);
            }
            finally
            {
                Environment.SetEnvironmentVariable(key.ToUpperInvariant(), original);
            }
        }

        [TestMethod]
        public void FactoryMethods_ReturnUsableInstances()
        {
            var utils = new TestUtilities();
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            var databaseName = $"{TestUtilities.GetKeyValue("CosmosIdentityDbName")}-utils-{Guid.NewGuid():N}";

            var options = utils.GetDbOptions(connectionString, databaseName);
            Assert.IsNotNull(options);

            using var containerUtils = utils.GetContainerUtilities(connectionString, databaseName);
            Assert.IsNotNull(containerUtils);

            using var dbContext = utils.GetDbContext(connectionString, databaseName);
            Assert.IsNotNull(dbContext);

            using var userStore = utils.GetUserStore(connectionString, databaseName);
            Assert.IsNotNull(userStore);

            using var roleStore = utils.GetRoleStore(connectionString, databaseName);
            Assert.IsNotNull(roleStore);

            using var roleManager = utils.GetRoleManager(connectionString, databaseName);
            Assert.IsNotNull(roleManager);

            ILogger<IdentityUser> logger = utils.GetLogger<IdentityUser>();
            Assert.IsNotNull(logger);
        }
    }
}
