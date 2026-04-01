using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass()]
    public class RoleManagerInterOperabilityTests : CosmosIdentityTestsBase
    {
        private static string connectionString;
        private static string databaseName;

        // Creates a new test role using the mock RoleManager to do so
        private async Task<IdentityRole> GetTestRole(RoleManager<IdentityRole> roleManager)
        {
            var role = await GetMockRandomRoleAsync(null, false);

            var result = await roleManager.CreateAsync(role);

            Assert.IsTrue(result.Succeeded);

            return await roleManager.FindByIdAsync(role.Id);
        }

        [ClassInitialize]
        public static async Task Initialize(TestContext context)
        {
            connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            databaseName = TestUtilities.GetKeyValue("CosmosIdentityDbName");
            InitializeClass(connectionString, databaseName);

            using var dbContext = _testUtilities.GetDbContext(connectionString, databaseName);

            // Clean up claims for test.
            var claims = await dbContext.RoleClaims.ToListAsync();
            var uclaims = await dbContext.UserClaims.ToListAsync();
            dbContext.RoleClaims.RemoveRange(claims);
            dbContext.UserClaims.RemoveRange(uclaims);
            await dbContext.SaveChangesAsync();

        }

        [TestMethod]
        public async Task Consolidated_ClaimsAsync_Tests()
        {
            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var claim = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Act - Add a claim
            var result1 = await roleManager.AddClaimAsync(role, claim);

            // Assert - Add a claim
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.GetClaimsAsync(role);
            // Two claims: LOCAL AUTHORITY and the new claim above
            Assert.AreEqual(1, result2?.Count);

            // Act - Remove a claim
            var result3 = await roleManager.RemoveClaimAsync(role, claim);

            // Assert
            Assert.IsTrue(result3.Succeeded);
            var result4 = await roleManager.GetClaimsAsync(role);
            Assert.AreEqual(0, result4.Count);
        }

        [TestMethod]
        public async Task CreateAsyncTest()
        {

            // Assert
            var roleStore = _testUtilities.GetRoleStore(connectionString, databaseName);
            using var roleManager = GetTestRoleManager(roleStore);
            var role = new IdentityRole();
            role.Name = Guid.NewGuid().ToString();
            role.NormalizedName = role.Name.ToLowerInvariant();
            role.Id = Guid.NewGuid().ToString();


            // Act
            var result1 = await roleManager.CreateAsync(role);


            // Assert
            Assert.IsTrue(result1.Succeeded);
        }

        [TestMethod]
        public async Task DeleteAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var id = role.Id;

            // Act
            var result1 = await roleManager.DeleteAsync(role);

            // Assert
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.FindByIdAsync(id);
            Assert.IsNull(result2);

        }
        [TestMethod]
        public async Task FindByIdAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var id = role.Id;

            // Act
            var result = await roleManager.FindByIdAsync(id);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(id, result.Id);

        }

        [TestMethod]
        public async Task FindByNameAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var name = role.Name;

            // Act
            var result = await roleManager.FindByNameAsync(name);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(name, result.Name);

        }
        [TestMethod]
        public async Task GetClaimsAsyncTest()
        {
            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var claim1 = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var claim2 = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var result1 = await roleManager.AddClaimAsync(role, claim1);
            var result2 = await roleManager.AddClaimAsync(role, claim2);
            Assert.IsTrue(result1.Succeeded);
            Assert.IsTrue(result2.Succeeded);

            // Act
            var result = await roleManager.GetClaimsAsync(role);

            // Assert
            Assert.AreEqual(2, result.Count);

        }

        [TestMethod]
        public async Task GetRoleIdAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);

            // Act
            var result = await roleManager.GetRoleIdAsync(role);

            // Assert
            Assert.AreEqual(role.Id, result);
        }

        [TestMethod]
        public async Task GetRoleNameAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);

            // Act
            var result = await roleManager.GetRoleNameAsync(role);

            // Assert
            Assert.AreEqual(role.Name, result);
        }

        [TestMethod]
        public async Task NormalizeKeyTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var key = Guid.NewGuid().ToString();

            // Act
            var result = roleManager.NormalizeKey(key);

            // Assert
            Assert.AreEqual(key.ToUpperInvariant(), result);
        }

        [TestMethod]
        public async Task RoleExistsAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);

            // Act
            var result = await roleManager.RoleExistsAsync(role.Name);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task SetRoleNameAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var name = Guid.NewGuid().ToString();

            // Act
            var result = await roleManager.SetRoleNameAsync(role, name);

            // Assert
            Assert.IsTrue(result.Succeeded);
            var result2 = await roleManager.FindByIdAsync(role.Id);
            Assert.AreEqual(name, result2.Name);

        }

        [TestMethod]
        public async Task UpdateAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);

            // Act

            // Assert

        }

        [TestMethod]
        public async Task UpdateNormalizedRoleNameAsyncTest()
        {
            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            var name = Guid.NewGuid().ToString();
            var result = await roleManager.SetRoleNameAsync(role, name);
            Assert.IsTrue(result.Succeeded);
            var result2 = await roleManager.FindByIdAsync(role.Id);
            Assert.AreEqual(name, result2.Name);

            // Act
            await roleManager.UpdateNormalizedRoleNameAsync(role);

            // Assert
            var result3 = await roleManager.FindByIdAsync(role.Id);
            Assert.AreEqual(name.ToUpperInvariant(), result3.NormalizedName);
        }

        [TestMethod]
        public async Task UpdateRoleAsyncTest()
        {

            // Assert
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var role = await GetTestRole(roleManager);
            role.Name = role.Name + "-A";
            role.NormalizedName = role.NormalizedName + "-A";
            var n = role.Name;
            var nn = role.NormalizedName;

            // Act
            var result1 = await roleManager.UpdateAsync(role);

            // Assert
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.FindByIdAsync(role.Id);
            Assert.AreEqual(n, result2.Name);
            Assert.AreEqual(nn, result2.NormalizedName);

        }

    }
}