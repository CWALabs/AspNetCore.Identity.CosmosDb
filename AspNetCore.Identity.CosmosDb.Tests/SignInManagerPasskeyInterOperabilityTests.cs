using Microsoft.AspNetCore.Identity;
using System.Threading;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class SignInManagerPasskeyInterOperabilityTests : CosmosIdentityTestsBase
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
        public async Task FindByPasskeyIdAsync_WithValidCredentialId_ReturnsCorrectUser()
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
            Assert.IsTrue(createResult.Succeeded, "User creation failed");

            var passkey = CreatePasskeyInfo("signin-test-passkey");
            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            // Find user by passkey credential ID
            var foundUser = await userStore.FindByPasskeyIdAsync(passkey.CredentialId, CancellationToken.None);

            Assert.IsNotNull(foundUser, "User should be found by passkey credential ID");
            Assert.AreEqual(user.Id, foundUser.Id, "Found user ID should match original user ID");
            Assert.AreEqual(user.Email, foundUser.Email, "Found user email should match");
        }

        [TestMethod]
        public async Task FindByPasskeyIdAsync_WithInvalidCredentialId_ReturnsNull()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);

            var invalidCredentialId = Guid.NewGuid().ToByteArray();

            var foundUser = await userStore.FindByPasskeyIdAsync(invalidCredentialId, CancellationToken.None);

            Assert.IsNull(foundUser, "User should not be found with non-existent passkey credential ID");
        }

        [TestMethod]
        public async Task GetPasskeysAsync_WithMultiplePasskeys_ReturnsAllPasskeys()
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
            var passkey3 = CreatePasskeyInfo("passkey-3");

            await userStore.AddOrUpdatePasskeyAsync(user, passkey1, CancellationToken.None);
            await userStore.AddOrUpdatePasskeyAsync(user, passkey2, CancellationToken.None);
            await userStore.AddOrUpdatePasskeyAsync(user, passkey3, CancellationToken.None);

            var passkeys = await userStore.GetPasskeysAsync(user, CancellationToken.None);

            Assert.IsNotNull(passkeys);
            Assert.AreEqual(3, passkeys.Count, "Should have exactly 3 passkeys");

            var names = passkeys.Select(p => p.Name).OrderBy(n => n).ToList();
            Assert.AreEqual("passkey-1", names[0]);
            Assert.AreEqual("passkey-2", names[1]);
            Assert.AreEqual("passkey-3", names[2]);
        }

        [TestMethod]
        public async Task UpdatePasskey_WithIncrementedSignCount_PreservesChanges()
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

            var passkey = CreatePasskeyInfo("counter-test");
            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            // Retrieve and verify initial state
            var retrieved = await userStore.FindPasskeyAsync(user, passkey.CredentialId, CancellationToken.None);
            Assert.IsNotNull(retrieved, "Passkey should be found");

            // Update with incremented counter
            var updatedPasskey = new UserPasskeyInfo(
                passkey.CredentialId,
                passkey.PublicKey,
                passkey.CreatedAt,
                5,  // Incremented sign count
                passkey.Transports,
                passkey.IsUserVerified,
                passkey.IsBackupEligible,
                passkey.IsBackedUp,
                passkey.AttestationObject,
                passkey.ClientDataJson)
            {
                Name = passkey.Name
            };

            await userStore.AddOrUpdatePasskeyAsync(user, updatedPasskey, CancellationToken.None);

            var updated = await userStore.FindPasskeyAsync(user, passkey.CredentialId, CancellationToken.None);
            Assert.AreEqual((long)5, updated.SignCount);
        }

        [TestMethod]
        public async Task RemovePasskey_AfterAddingMultiple_OnlyRemovesSpecificPasskey()
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

            var passkey1 = CreatePasskeyInfo("keep-me");
            var passkey2 = CreatePasskeyInfo("delete-me");

            await userStore.AddOrUpdatePasskeyAsync(user, passkey1, CancellationToken.None);
            await userStore.AddOrUpdatePasskeyAsync(user, passkey2, CancellationToken.None);

            var passkeysBeforeDelete = await userStore.GetPasskeysAsync(user, CancellationToken.None);
            Assert.AreEqual(2, passkeysBeforeDelete.Count);

            // Remove only passkey2
            await userStore.RemovePasskeyAsync(user, passkey2.CredentialId, CancellationToken.None);

            var passkeysAfterDelete = await userStore.GetPasskeysAsync(user, CancellationToken.None);
            Assert.AreEqual(1, passkeysAfterDelete.Count);
            Assert.AreEqual("keep-me", passkeysAfterDelete[0].Name);

            // Verify passkey2 is gone
            var deletedPasskey = await userStore.FindPasskeyAsync(user, passkey2.CredentialId, CancellationToken.None);
            Assert.IsNull(deletedPasskey);
        }

        [TestMethod]
        public async Task PasskeyBackupFlags_ArePreservedOnUpdate()
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

            // Create with initial backup flags
            var credentialId = Guid.NewGuid().ToByteArray();
            var publicKey = Guid.NewGuid().ToByteArray();
            var attestationObject = Guid.NewGuid().ToByteArray();
            var clientDataJson = Guid.NewGuid().ToByteArray();

            var passkey = new UserPasskeyInfo(
                credentialId,
                publicKey,
                DateTimeOffset.UtcNow,
                0,
                new[] { "internal" },
                isUserVerified: true,
                isBackupEligible: true,
                isBackedUp: false,
                attestationObject,
                clientDataJson)
            {
                Name = "backup-test"
            };

            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var retrieved = await userStore.FindPasskeyAsync(user, credentialId, CancellationToken.None);
            Assert.IsTrue(retrieved.IsBackupEligible, "IsBackupEligible should be true");
            Assert.IsFalse(retrieved.IsBackedUp, "IsBackedUp should be false");

            // Create an updated passkey with new backup status
            var updatedPasskey = new UserPasskeyInfo(
                credentialId,
                publicKey,
                DateTimeOffset.UtcNow,
                0,
                new[] { "internal" },
                isUserVerified: true,
                isBackupEligible: true,
                isBackedUp: true,
                attestationObject,
                clientDataJson)
            {
                Name = "backup-test"
            };

            await userStore.AddOrUpdatePasskeyAsync(user, updatedPasskey, CancellationToken.None);

            var updated = await userStore.FindPasskeyAsync(user, credentialId, CancellationToken.None);
            Assert.IsTrue(updated.IsBackupEligible, "IsBackupEligible should remain true");
            Assert.IsTrue(updated.IsBackedUp, "IsBackedUp should be updated to true");
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
