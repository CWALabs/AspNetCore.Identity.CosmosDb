using Microsoft.AspNetCore.Identity;
using System.Threading;

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

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var passkey = CreatePasskeyInfo("store-passkey");

            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var foundUser = await userStore.FindByPasskeyIdAsync(passkey.CredentialId, CancellationToken.None);
            Assert.IsNotNull(foundUser);
            Assert.AreEqual(user.Id, foundUser.Id);

            await userStore.RemovePasskeyAsync(user, passkey.CredentialId, CancellationToken.None);

            var removedUser = await userStore.FindByPasskeyIdAsync(passkey.CredentialId, CancellationToken.None);
            Assert.IsNull(removedUser);
        }

        [TestMethod]
        public async Task AddPasskey_WithUserVerification_StoresUserVerificationFlag()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var credentialId = Guid.NewGuid().ToByteArray();
            var publicKey = Guid.NewGuid().ToByteArray();
            var attestationObject = Guid.NewGuid().ToByteArray();
            var clientDataJson = Guid.NewGuid().ToByteArray();

            var passkey = new UserPasskeyInfo(
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
                Name = "verified-passkey"
            };

            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var storedPasskey = await userStore.FindPasskeyAsync(user, credentialId, CancellationToken.None);
            Assert.IsNotNull(storedPasskey);
            Assert.IsTrue(storedPasskey.IsUserVerified);
            Assert.AreEqual("verified-passkey", storedPasskey.Name);
        }

        [TestMethod]
        public async Task GetPasskeys_ReturnsEmptyList_WhenNoPasskeysExist()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var passkeys = await userStore.GetPasskeysAsync(user, CancellationToken.None);
            Assert.IsNotNull(passkeys);
            Assert.AreEqual(0, passkeys.Count);
        }

        [TestMethod]
        public async Task GetPasskeys_ReturnsMultiplePasskeys()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var passkey1 = CreatePasskeyInfo("passkey-1");
            var passkey2 = CreatePasskeyInfo("passkey-2");

            await userStore.AddOrUpdatePasskeyAsync(user, passkey1, CancellationToken.None);
            await userStore.AddOrUpdatePasskeyAsync(user, passkey2, CancellationToken.None);

            var passkeys = await userStore.GetPasskeysAsync(user, CancellationToken.None);
            Assert.AreEqual(2, passkeys.Count);
        }

        [TestMethod]
        public async Task FindPasskey_ReturnsNullForNonExistentCredentialId()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var nonExistentCredentialId = Guid.NewGuid().ToByteArray();
            var result = await userStore.FindPasskeyAsync(user, nonExistentCredentialId, CancellationToken.None);
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task UpdatePasskey_ModifiesExistingPasskey()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var randomEmail = $"{Guid.NewGuid()}@example.com";
            var user = new IdentityUser(randomEmail)
            {
                Email = randomEmail,
                Id = Guid.NewGuid().ToString(),
                NormalizedUserName = randomEmail.ToUpperInvariant(),
                NormalizedEmail = randomEmail.ToUpperInvariant()
            };

            var createResult = await userStore.CreateAsync(user);
            Assert.IsTrue(createResult.Succeeded);

            var initialPasskey = CreatePasskeyInfo("initial-name");
            await userStore.AddOrUpdatePasskeyAsync(user, initialPasskey, CancellationToken.None);

            // Update the passkey with same credential ID but new name/data
            var updatedPasskey = new UserPasskeyInfo(
                initialPasskey.CredentialId,
                initialPasskey.PublicKey,
                initialPasskey.CreatedAt,
                2,  // Incremented sign count
                initialPasskey.Transports,
                initialPasskey.IsUserVerified,
                initialPasskey.IsBackupEligible,
                initialPasskey.IsBackedUp,
                initialPasskey.AttestationObject,
                initialPasskey.ClientDataJson)
            {
                Name = "updated-name"
            };

            await userStore.AddOrUpdatePasskeyAsync(user, updatedPasskey, CancellationToken.None);

            var storedPasskey = await userStore.FindPasskeyAsync(user, initialPasskey.CredentialId, CancellationToken.None);
            Assert.IsNotNull(storedPasskey);
            Assert.AreEqual("updated-name", storedPasskey.Name);
            Assert.AreEqual((long)2, storedPasskey.SignCount);
        }

        /// <summary>
        /// Helper method to create a UserPasskeyInfo for testing
        /// </summary>
        private UserPasskeyInfo CreatePasskeyInfo(string name, uint signCount = 0)
        {
            var credentialId = Guid.NewGuid().ToByteArray();
            var publicKey = Guid.NewGuid().ToByteArray();
            var attestationObject = Guid.NewGuid().ToByteArray();
            var clientDataJson = Guid.NewGuid().ToByteArray();

            return new UserPasskeyInfo(
                credentialId,
                publicKey,
                DateTimeOffset.UtcNow,
                signCount,
                new[] { "internal" },
                isUserVerified: false,
                isBackupEligible: false,
                isBackedUp: false,
                attestationObject,
                clientDataJson)
            {
                Name = name
            };
        }
    }
}
