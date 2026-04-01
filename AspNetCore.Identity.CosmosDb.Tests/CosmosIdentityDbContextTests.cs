using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    [DoNotParallelize]
    public class CosmosIdentityDbContextTests
    {
        [TestMethod]
        public void ModelBuild_WithDefaultStoreOptions_DoesNotThrow()
        {
            var utils = new TestUtilities();
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            var databaseName = $"{TestUtilities.GetKeyValue("CosmosIdentityDbName")}-ctx-{Guid.NewGuid():N}";

            using var dbContext = new CosmosIdentityDbContext<IdentityUser, IdentityRole, string>(
                utils.GetDbOptions(connectionString, databaseName),
                backwardCompatibility: false);

            var model = dbContext.Model;

            Assert.IsNotNull(model.FindEntityType(typeof(IdentityUser)));
            Assert.IsNotNull(model.FindEntityType(typeof(IdentityRole)));
        }

        [TestMethod]
        public void ModelBuild_WithConfiguredStoreOptionsAndBackwardCompatibility_DoesNotThrow()
        {
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            var databaseName = $"{TestUtilities.GetKeyValue("CosmosIdentityDbName")}-ctx-{Guid.NewGuid():N}";

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<IdentityOptions>(opts =>
            {
                opts.Stores.MaxLengthForKeys = 64;
                opts.Stores.ProtectPersonalData = true;
            });
            services.AddSingleton<IPersonalDataProtector, TestProtector>();

            var provider = services.BuildServiceProvider();

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseCosmos(connectionString, databaseName);
            optionsBuilder.UseApplicationServiceProvider(provider);

            using var dbContext = new CosmosIdentityDbContext<IdentityUser, IdentityRole, string>(
                optionsBuilder.Options,
                backwardCompatibility: true);

            var model = dbContext.Model;

            Assert.IsNotNull(model.FindEntityType(typeof(IdentityUser)));
            Assert.IsNotNull(model.FindEntityType(typeof(IdentityRole)));
        }

        [TestMethod]
        public void GetStoreOptions_ReturnsConfiguredOptions()
        {
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            var databaseName = $"{TestUtilities.GetKeyValue("CosmosIdentityDbName")}-ctx-{Guid.NewGuid():N}";

            var services = new ServiceCollection();
            services.AddOptions();
            services.Configure<IdentityOptions>(opts =>
            {
                opts.Stores.MaxLengthForKeys = 32;
                opts.Stores.ProtectPersonalData = false;
            });

            var provider = services.BuildServiceProvider();

            var optionsBuilder = new DbContextOptionsBuilder();
            optionsBuilder.UseCosmos(connectionString, databaseName);
            optionsBuilder.UseApplicationServiceProvider(provider);

            using var dbContext = new CosmosIdentityDbContext<IdentityUser, IdentityRole, string>(optionsBuilder.Options);

            var method = typeof(CosmosIdentityDbContext<IdentityUser, IdentityRole, string>)
                .GetMethod("GetStoreOptions", BindingFlags.Instance | BindingFlags.NonPublic);

            var storeOptions = method?.Invoke(dbContext, null) as StoreOptions;

            Assert.IsNotNull(storeOptions);
            Assert.AreEqual(32, storeOptions.MaxLengthForKeys);
            Assert.IsFalse(storeOptions.ProtectPersonalData);
        }

        private sealed class TestProtector : IPersonalDataProtector
        {
            public string Protect(string data) => $"enc:{data}";

            public string Unprotect(string data) => data.Replace("enc:", string.Empty);
        }
    }
}
