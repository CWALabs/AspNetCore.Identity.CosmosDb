using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class UserManagerPasskeyInterOperabilityTests : CosmosIdentityTestsBase
    {
        private static string connectionString;
        private static string databaseName;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            databaseName = TestUtilities.GetKeyValue("CosmosIdentityDbName");
            InitializeClass(connectionString, databaseName);
        }

        [TestMethod]
        public async Task SetFindAndRemovePasskeyAsyncTest()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);
            using var userManager = GetTestUserManager(userStore);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userManager.CreateAsync(user, $"A1a{Guid.NewGuid()}");
            Assert.IsTrue(createResult.Succeeded);

            var passkey = CreatePasskeyInfo("manager-passkey");

            var setResult = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
            Assert.IsTrue(setResult.Succeeded);

            var foundUser = await userManager.FindByPasskeyIdAsync(passkey.CredentialId);
            Assert.IsNotNull(foundUser);
            Assert.AreEqual(user.Id, foundUser.Id);

            var removeResult = await userManager.RemovePasskeyAsync(user, passkey.CredentialId);
            Assert.IsTrue(removeResult.Succeeded);

            var removedUser = await userManager.FindByPasskeyIdAsync(passkey.CredentialId);
            Assert.IsNull(removedUser);
        }

        private static UserPasskeyInfo CreatePasskeyInfo(string name)
        {
            var credentialId = Guid.NewGuid().ToByteArray();
            var publicKey = Guid.NewGuid().ToByteArray();
            var attestationObject = Guid.NewGuid().ToByteArray();
            var clientDataJson = Guid.NewGuid().ToByteArray();

            return new UserPasskeyInfo(
                credentialId,
                publicKey,
                DateTimeOffset.UtcNow,
                1,
                new[] { "internal" },
                isUserVerified: true,
                isBackupEligible: true,
                isBackedUp: false,
                attestationObject,
                clientDataJson)
            {
                Name = name
            };
        }
    }
}
