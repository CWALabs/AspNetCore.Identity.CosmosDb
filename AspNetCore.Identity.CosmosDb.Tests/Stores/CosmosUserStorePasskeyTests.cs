using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9.Stores
{
    [TestClass]
    public class CosmosUserStorePasskeyTests : CosmosIdentityTestsBase
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
        public async Task AddOrUpdatePasskeyAndGetPasskeysAsyncTest()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);
            var user = await GetMockRandomUserAsync(userStore);

            var passkey = CreatePasskeyInfo("first-passkey");

            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var passkeys = await userStore.GetPasskeysAsync(user, CancellationToken.None);

            Assert.IsNotNull(passkeys);
            Assert.IsTrue(passkeys.Count >= 1);

            var persisted = passkeys.SingleOrDefault(p => p.CredentialId.SequenceEqual(passkey.CredentialId));
            Assert.IsNotNull(persisted);
            Assert.AreEqual("first-passkey", persisted.Name);

            passkey.Name = "updated-passkey";
            passkey.SignCount += 1;
            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var passkeysAfterUpdate = await userStore.GetPasskeysAsync(user, CancellationToken.None);
            var updated = passkeysAfterUpdate.Single(p => p.CredentialId.SequenceEqual(passkey.CredentialId));
            Assert.AreEqual("updated-passkey", updated.Name);
            Assert.AreEqual(passkey.SignCount, updated.SignCount);
        }

        [TestMethod]
        public async Task FindByPasskeyIdAndFindPasskeyAsyncTest()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);
            var user = await GetMockRandomUserAsync(userStore);

            var passkey = CreatePasskeyInfo("lookup-passkey");
            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            var foundUser = await userStore.FindByPasskeyIdAsync(passkey.CredentialId, CancellationToken.None);
            Assert.IsNotNull(foundUser);
            Assert.AreEqual(user.Id, foundUser.Id);

            var foundPasskey = await userStore.FindPasskeyAsync(user, passkey.CredentialId, CancellationToken.None);
            Assert.IsNotNull(foundPasskey);
            Assert.IsTrue(foundPasskey.CredentialId.SequenceEqual(passkey.CredentialId));
            Assert.AreEqual("lookup-passkey", foundPasskey.Name);
        }

        [TestMethod]
        public async Task RemovePasskeyAsyncTest()
        {
            using var userStore = _testUtilities.GetUserStore(connectionString, databaseName);
            var user = await GetMockRandomUserAsync(userStore);

            var passkey = CreatePasskeyInfo("to-delete");
            await userStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

            await userStore.RemovePasskeyAsync(user, passkey.CredentialId, CancellationToken.None);

            var removed = await userStore.FindPasskeyAsync(user, passkey.CredentialId, CancellationToken.None);
            Assert.IsNull(removed);
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
