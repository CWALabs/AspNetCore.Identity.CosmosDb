using AspNetCore.Identity.CosmosDb.Contracts;
using AspNetCore.Identity.CosmosDb.Extensions;
using AspNetCore.Identity.CosmosDb.Repositories;
using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class ServiceCollectionExtensionsTests
    {
        [TestMethod]
        public void AddCosmosIdentity_WithoutDbContextOverload_ReturnsBuilder()
        {
            var services = new ServiceCollection();

            var builder = services.AddCosmosIdentity<IdentityUser, IdentityRole, string>();

            Assert.IsNotNull(builder);
            Assert.AreEqual(typeof(IdentityUser), builder.UserType);
            Assert.AreEqual(typeof(IdentityRole), builder.RoleType);
        }

        [TestMethod]
        public void AddCosmosIdentity_RegistersExpectedStoreServices()
        {
            var services = new ServiceCollection();

            services.AddCosmosIdentity<CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IdentityUser, IdentityRole, string>(_ =>
            {
            });

            var userStore = services.FirstOrDefault(_ => _.ServiceType == typeof(IUserStore<IdentityUser>));
            var roleStore = services.FirstOrDefault(_ => _.ServiceType == typeof(IRoleStore<IdentityRole>));
            var repository = services.FirstOrDefault(_ => _.ServiceType == typeof(IRepository));

            Assert.IsNotNull(userStore);
            Assert.IsNotNull(roleStore);
            Assert.IsNotNull(repository);

            Assert.AreEqual(typeof(CosmosUserStore<IdentityUser, IdentityRole, string>), userStore.ImplementationType);
            Assert.AreEqual(typeof(CosmosRoleStore<IdentityRole, string>), roleStore.ImplementationType);
            Assert.AreEqual(typeof(CosmosIdentityRepository<CosmosIdentityDbContext<IdentityUser, IdentityRole, string>, IdentityUser, IdentityRole, string>), repository.ImplementationType);
        }
    }
}
