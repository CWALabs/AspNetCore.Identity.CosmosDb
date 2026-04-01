using System.Security.Claims;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9.Stores
{
    [TestClass()]
    public class CosmosUserStoreClaimsTests : CosmosIdentityTestsBase
    {

        //private static TestUtilities? utils;
        //private static CosmosUserStore<IdentityUser>? userStore;
        //private static CosmosRoleStore<IdentityRole>? _roleStore;
        // private static string phoneNumber = "0000000000";
        //private static Random? _random;


        private static string connectionString;
        private static string databaseName;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            databaseName = TestUtilities.GetKeyValue("CosmosIdentityDbName");
            InitializeClass(connectionString, databaseName);
        }

        #region methods implementing IUserClaimStore<TUserEntity>

        [TestMethod()]
        public async Task Consolidated_ClaimsAsync_CRUD_Tests()
        {
            // Arrange
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);
            var user1 = await GetMockRandomUserAsync(userStore);

            // Clean up claims before starting
            var claims = await userStore.GetClaimsAsync(user1, default);
            if (claims.Any())
            {
                await userStore.RemoveClaimsAsync(user1, claims, default);
            }

            var claim = new Claim[] { GetMockClaim("a"), GetMockClaim("b"), GetMockClaim("c") };
            var newClaim = GetMockClaim("d");

            await userStore.AddClaimsAsync(user1, claim, default);

            // Act - Create
            var result2 = await userStore.GetClaimsAsync(user1, default);

            // Assert - Create
            Assert.AreEqual(3, result2.Count);

            // Act - Replace
            await userStore.ReplaceClaimAsync(user1, claim.FirstOrDefault(), newClaim, default);

            // test - Replace
            var result3 = await userStore.GetClaimsAsync(user1, default);
            Assert.IsFalse(result3.Any(a => a.Type == claim.FirstOrDefault().Type));

            var testAny = result3.Any(a => a.Type == newClaim.Type);
            if (!testAny)
            {
                throw new Exception($"Replace failed with {result3.Count} with types { string.Join(",", result3.Select(s => s.Type).ToArray()) }).");
            }

            Assert.IsTrue(testAny);

            // Act - Delete
            await userStore.RemoveClaimsAsync(user1, result3, default);
            var result4 = await userStore.GetClaimsAsync(user1, default);
            Assert.IsFalse(result4.Any());
        }

        [TestMethod()]
        public async Task GetUsersForClaimAsyncTest()
        {
            // Arrange
            var val = Guid.NewGuid().ToString();
            var claims = new Claim[] { new Claim(val, val) };
            using (var userStore = _testUtilities.GetUserStore(connectionString, databaseName))
            {
                var user1 = await GetMockRandomUserAsync(userStore);
                await userStore.AddClaimsAsync(user1, claims, default);
            }

            using (var userStore = _testUtilities.GetUserStore(connectionString, databaseName))
            {
                var user2 = await GetMockRandomUserAsync(userStore);
                await userStore.AddClaimsAsync(user2, claims, default);
            }

            using (var userStore = _testUtilities.GetUserStore(connectionString, databaseName))
            {
                // Act
                var result1 = await userStore.GetUsersForClaimAsync(claims.FirstOrDefault(), default);
                // Assert
                Assert.AreEqual(2, result1.Count);
            }

        }

        #endregion
    }
}