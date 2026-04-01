using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    /// <summary>
    /// Tests the <see cref="UserManager{TUser}"/> when hooked up to Cosmos user and role stores.
    /// </summary>
    [TestClass]
    public class UserManagerInterOperabilityTests : CosmosIdentityTestsBase
    {
        private static string connectionString;
        private static string databaseName;

        // Creates a new test user with a hashed password, using the mock UserManager to do so
        private async Task<IdentityUser> GetTestUser(UserManager<IdentityUser> userManager, string password = "")
        {
            var user = await GetMockRandomUserAsync(null, false);

            if (string.IsNullOrEmpty(password))
                password = $"A1a{Guid.NewGuid()}";

            var result = await userManager.CreateAsync(user, password);

            Assert.IsTrue(result.Succeeded);
            return await userManager.FindByIdAsync(user.Id);
        }

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            databaseName = TestUtilities.GetKeyValue("CosmosIdentityDbName");
            InitializeClass(connectionString, databaseName);
        }

        [TestMethod]
        public async Task GetUserNameTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.GetUserNameAsync(user);
        }

        [TestMethod]
        public async Task GetUserIdTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result2 = await userManager.GetUserIdAsync(user);
        }

        [TestMethod]
        public async Task CreateAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetMockRandomUserAsync(null, false);

            // Act
            var result = await userManager.CreateAsync(user);

            // Assert
            var result2 = await userManager.FindByIdAsync(user.Id);
            Assert.IsTrue(user.Id == result2.Id);
        }

        [TestMethod]
        public async Task UpdateAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            user.PhoneNumber = "9998884444";

            // Act
            var result1 = await userManager.UpdateAsync(user);

            // Assert
            user = await userManager.FindByIdAsync(user.Id);
            Assert.AreEqual("9998884444", user.PhoneNumber);

        }

        [TestMethod]
        public async Task DeleteAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var id = user.Id;
            user = await userManager.FindByIdAsync(id);
            Assert.IsNotNull(user);

            // Act
            var result = await userManager.DeleteAsync(user);

            // Assert
            Assert.IsTrue(result.Succeeded);
            user = await userManager.FindByIdAsync(id);
            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task FindByIdAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            user = await userManager.FindByIdAsync(user.Id);

            // Assert
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public async Task FindByNameAsync()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            user = await userManager.FindByNameAsync(user.UserName);

            // Assert
            Assert.IsNotNull(user);
        }

        [TestMethod]
        public async Task CreateAsync_WithPassword_Test()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));

            // Act
            var user = await GetTestUser(userManager);

            // Assert
            var result = await userManager.HasPasswordAsync(user);
            Assert.IsNotNull(user);
            Assert.IsTrue(result);
            Assert.IsTrue(!string.IsNullOrEmpty(user.PasswordHash));
        }

        [TestMethod]
        public async Task UpdateNormalizedUserNameAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var userName = "Az" + user.UserName;
            user.UserName = userName;

            // Act
            await userManager.UpdateNormalizedUserNameAsync(user);

            // Assert
            user = await userManager.FindByIdAsync(user.Id);
            Assert.IsTrue(user.NormalizedUserName == userName.ToUpperInvariant());
        }

        [TestMethod]
        public async Task GetUserNameAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var userName = await userManager.GetUserNameAsync(user);

            // Assert
            Assert.AreEqual(user.UserName, userName);
        }

        [TestMethod]
        public async Task SetUserNameAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var userName = "Az" + user.UserName;

            // Act
            await userManager.SetUserNameAsync(user, userName);

            // Assert
            user = await userManager.FindByIdAsync(user.Id);
            Assert.IsTrue(user.UserName == userName);
        }

        [TestMethod]
        public async Task GetUserIdAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.GetUserIdAsync(user);

            // Assert
            Assert.AreEqual(user.Id, result);
        }

        [TestMethod]
        public async Task CheckPasswordAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var originalPassword = $"A1a{Guid.NewGuid()}";
            var user = await GetTestUser(userManager, originalPassword);

            // Act - fail
            var result = await userManager.ChangePasswordAsync(user, originalPassword, Guid.NewGuid().ToString());

            // Assert - fail
            Assert.IsFalse(result.Succeeded);

            // Act - succeed
            result = await userManager.ChangePasswordAsync(user, originalPassword, $"A1a{Guid.NewGuid()}");

            // Assert - succeed
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public async Task HasPasswordAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.HasPasswordAsync(user);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task AddPasswordAsyncTest()
        {
            // Arrange - fail
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.AddPasswordAsync(user, $"A1a{Guid.NewGuid()}");

            // Assert
            Assert.IsFalse(result.Succeeded); // Already has a password

            // Arrange - success
            user.PasswordHash = null;
            var result2 = await userManager.UpdateAsync(user);
            user = await userManager.FindByIdAsync(user.Id);

            // Act
            var result3 = await userManager.AddPasswordAsync(user, $"A1a{Guid.NewGuid()}");

            // Assert
            Assert.IsTrue(result3.Succeeded); // Already has a password
        }

        [TestMethod]
        public async Task ChangePasswordAsyncTest()
        {
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var originalPassword = $"A1a{Guid.NewGuid()}";
            var user = await GetTestUser(userManager, originalPassword);

            // Act
            var result = await userManager.ChangePasswordAsync(user, originalPassword, $"A1a{Guid.NewGuid()}");

            // Assert
            Assert.IsTrue(result.Succeeded);
        }

        [TestMethod]
        public async Task RemovePasswordAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            Assert.IsTrue(await userManager.HasPasswordAsync(user));

            // Act
            var result = await userManager.RemovePasswordAsync(user);

            // Assert
            Assert.IsTrue(result.Succeeded);
            Assert.IsFalse(await userManager.HasPasswordAsync(user));
        }

        [TestMethod]
        public async Task GetSecurityStampAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.GetSecurityStampAsync(user);

            // Assert
            Assert.IsFalse(string.IsNullOrEmpty(result));
            Assert.AreEqual(result, user.SecurityStamp);
        }

        [TestMethod]
        public async Task UpdateSecurityStampAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var stamp1 = user.SecurityStamp;

            // Act
            var result = await userManager.UpdateSecurityStampAsync(user);

            // Assert
            user = await userManager.FindByIdAsync(user.Id);
            Assert.IsTrue(result.Succeeded);
            Assert.AreNotEqual(stamp1, user.SecurityStamp);
        }

        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task GeneratePasswordResetTokenAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);

        //    // Act
        //    var result = await userManager.GeneratePasswordResetTokenAsync(user);

        //    // Assert
        //    Assert.IsFalse(string.IsNullOrEmpty(result));
        //}

        // TODO: Needs IUserTwoFactorTokenProvider<TUser> registered to work.
        //[TestMethod]
        //public async Task ResetPasswordAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);
        //    var token = await userManager.GeneratePasswordResetTokenAsync(user);
        //    var password = $"A1a{Guid.NewGuid()}";

        //    // Act
        //    var result = await userManager.ResetPasswordAsync(user, token, password);

        //    // Assert
        //    Assert.IsTrue(result.Succeeded);
        //}

        [TestMethod]
        public async Task FindByLoginAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var loginInfo = GetMockLoginInfoAsync();
            await userManager.AddLoginAsync(user, loginInfo);
            var logins = await userManager.GetLoginsAsync(user);
            Assert.AreEqual(1, logins.Count);
            Assert.IsTrue(logins.Any(a => a.LoginProvider.Equals("Twitter")));

            // Act
            var user2 = await userManager.FindByLoginAsync("Twitter", loginInfo.ProviderKey);

            // Assert
            Assert.AreEqual(user.Id, user2.Id);

        }

        [TestMethod]
        public async Task RemoveLoginAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var loginInfo = GetMockLoginInfoAsync();
            await userManager.AddLoginAsync(user, loginInfo);
            var logins = await userManager.GetLoginsAsync(user);
            Assert.AreEqual(1, logins.Count);
            Assert.IsTrue(logins.Any(a => a.LoginProvider.Equals("Twitter")));
            var user2 = await userManager.FindByLoginAsync("Twitter", loginInfo.ProviderKey);
            Assert.AreEqual(user.Id, user2.Id);

            // Act
            var result = await userManager.RemoveLoginAsync(user, "Twitter", loginInfo.ProviderKey);

            // Assert
            Assert.IsTrue(result.Succeeded);
            logins = await userManager.GetLoginsAsync(user);
            Assert.AreEqual(0, logins.Count);
        }

        [TestMethod]
        public async Task AddLoginAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var loginInfo = GetMockLoginInfoAsync();

            // Act
            await userManager.AddLoginAsync(user, loginInfo);

            // Assert
            var logins = await userManager.GetLoginsAsync(user);
            Assert.AreEqual(1, logins.Count);
            Assert.IsTrue(logins.Any(a => a.LoginProvider.Equals("Twitter")));
        }

        [TestMethod]
        public async Task GetLoginsAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var loginInfo = GetMockLoginInfoAsync();
            await userManager.AddLoginAsync(user, loginInfo);

            // Act
            var logins = await userManager.GetLoginsAsync(user);

            // Assert
            Assert.AreEqual(1, logins.Count);
            Assert.IsTrue(logins.Any(a => a.LoginProvider.Equals("Twitter")));
        }

        [TestMethod]
        public async Task AddClaimAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claim = new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            // Act
            var result = await userManager.AddClaimAsync(user, claim);

            // Assert
            Assert.IsTrue(result.Succeeded);
            var result2 = await userManager.GetClaimsAsync(user);
            Assert.AreEqual(1, result2.Count);
        }

        [TestMethod]
        public async Task AddClaimsAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claims = new Claim[] { new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()), new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()), new Claim(Guid.NewGuid().ToString(), Guid.NewGuid().ToString()) };

            // Act
            var result = await userManager.AddClaimsAsync(user, claims);

            // Assert
            Assert.IsTrue(result.Succeeded);
            claims = (await userManager.GetClaimsAsync(user)).ToArray();
            Assert.AreEqual(3, claims.Count());
        }

        [TestMethod]
        public async Task ReplaceClaimAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claim = new Claim("1", "1");
            var newClaim = new Claim("1", "2");
            var result1 = await userManager.AddClaimAsync(user, claim);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.ReplaceClaimAsync(user, claim, newClaim);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            var result3 = await userManager.GetClaimsAsync(user);
            Assert.AreEqual(1, result3.Count);
        }

        [TestMethod]
        public async Task RemoveClaimAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claim = new Claim("1", "1");
            var result1 = await userManager.AddClaimAsync(user, claim);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.RemoveClaimAsync(user, claim);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            var result3 = await userManager.GetClaimsAsync(user);
            Assert.AreEqual(0, result3.Count);
        }

        [TestMethod]
        public async Task RemoveClaimsAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
            var result1 = await userManager.AddClaimsAsync(user, claims);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.RemoveClaimsAsync(user, claims);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            claims = (await userManager.GetClaimsAsync(user)).ToArray();
            Assert.AreEqual(0, claims.Count());
        }

        [TestMethod]
        public async Task GetClaimsAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var claims = new Claim[] { new Claim("1", "1"), new Claim("2", "2"), new Claim("3", "3") };
            var result1 = await userManager.AddClaimsAsync(user, claims);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.GetClaimsAsync(user);

            // Assert
            Assert.AreEqual(3, result2.Count);
        }

        [TestMethod]
        public async Task AddToRoleAsyncTest()
        {
            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.AddToRoleAsync(user, role.Name);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            var result3 = await userManager.GetRolesAsync(user);
            Assert.AreEqual(1, result3.Count);
        }

        [TestMethod]
        public async Task AddToRolesAsyncTest()
        {
            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role1 = await GetMockRandomRoleAsync(null, false);
            var role2 = await GetMockRandomRoleAsync(null, false);
            var role3 = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role1);
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.CreateAsync(role2);
            Assert.IsTrue(result2.Succeeded);
            var result3 = await roleManager.CreateAsync(role3);
            Assert.IsTrue(result3.Succeeded);
            var roles = new string[] { role1.Name, role2.Name, role3.Name };

            // Act
            var result4 = await userManager.AddToRolesAsync(user, roles);

            // Assert
            Assert.IsTrue(result4.Succeeded);
            var result5 = await userManager.GetRolesAsync(user);
            Assert.AreEqual(3, result5.Count);
        }

        [TestMethod]
        public async Task RemoveFromRoleAsyncTest()
        {
            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role);
            Assert.IsTrue(result1.Succeeded);
            Assert.IsTrue((await userManager.AddToRoleAsync(user, role.Name)).Succeeded);

            // Act
            var result2 = await userManager.RemoveFromRoleAsync(user, role.Name);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            var result3 = await userManager.GetRolesAsync(user);
            Assert.AreEqual(0, result3.Count);
        }

        [TestMethod]
        public async Task RemoveFromRolesAsyncTest()
        {

            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role1 = await GetMockRandomRoleAsync(null, false);
            var role2 = await GetMockRandomRoleAsync(null, false);
            var role3 = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role1);
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.CreateAsync(role2);
            Assert.IsTrue(result2.Succeeded);
            var result3 = await roleManager.CreateAsync(role3);
            Assert.IsTrue(result3.Succeeded);
            var roles = new string[] { role1.Name, role2.Name, role3.Name };

            // Act
            var result5 = await userManager.RemoveFromRolesAsync(user, roles);

            // Assert
            Assert.IsTrue(result2.Succeeded);
            var result6 = await userManager.GetRolesAsync(user);
            Assert.AreEqual(0, result6.Count);
        }

        [TestMethod]
        public async Task GetRolesAsyncTest()
        {

            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role1 = await GetMockRandomRoleAsync(null, false);
            var role2 = await GetMockRandomRoleAsync(null, false);
            var role3 = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role1);
            Assert.IsTrue(result1.Succeeded);
            var result2 = await roleManager.CreateAsync(role2);
            Assert.IsTrue(result2.Succeeded);
            var result3 = await roleManager.CreateAsync(role3);
            Assert.IsTrue(result3.Succeeded);
            var roles = new string[] { role1.Name, role2.Name, role3.Name };
            Assert.IsTrue((await userManager.AddToRolesAsync(user, roles)).Succeeded);

            // Act
            var result5 = await userManager.GetRolesAsync(user);

            // Assert
            Assert.AreEqual(3, result5.Count);
        }

        [TestMethod]
        public async Task IsInRoleAsyncTest()
        {
            // Arrange
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var role = await GetMockRandomRoleAsync(null, false);
            var result1 = await roleManager.CreateAsync(role);
            Assert.IsTrue(result1.Succeeded);
            Assert.IsTrue((await userManager.AddToRoleAsync(user, role.Name)).Succeeded);

            // Act
            var result2 = await userManager.IsInRoleAsync(user, role.Name);

            // Assert
            Assert.IsTrue(result2);
            var result3 = await userManager.GetRolesAsync(user);
            Assert.AreEqual(1, result3.Count);
        }

        [TestMethod]
        public async Task GetEmailAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result1 = await userManager.GetEmailAsync(user);

            // Assert
            Assert.AreEqual(user.Email, result1);

        }

        [TestMethod]
        public async Task SetEmailAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var emailAddress = "bb" + user.Email;

            // Act
            var result1 = await userManager.SetEmailAsync(user, emailAddress);

            // Assert
            Assert.IsTrue(result1.Succeeded);
            var user2 = await userManager.FindByIdAsync(user.Id);
            Assert.AreEqual(emailAddress, user2.Email);
        }

        [TestMethod]
        public async Task FindByEmailAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result1 = await userManager.FindByEmailAsync(user.Email);

            // Assert
            Assert.AreEqual(user.Id, result1.Id);
        }

        [TestMethod]
        public async Task UpdateNormalizedEmailAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var emailAddress = "Bb" + user.Email;
            user.Email = emailAddress;

            // Act
            await userManager.UpdateNormalizedEmailAsync(user);

            // Assert
            var user2 = await userManager.FindByIdAsync(user.Id);
            Assert.AreEqual(emailAddress.ToUpperInvariant(), user2.NormalizedEmail);
        }

        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task GenerateEmailConfirmationTokenAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);

        //    // Act
        //    var result1 = await userManager.GenerateEmailConfirmationTokenAsync(user);

        //    // Assert
        //    Assert.IsFalse(string.IsNullOrEmpty(result1));
        //}


        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task ConfirmEmailAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);
        //    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        //    var result1 = await userManager.IsEmailConfirmedAsync(user);
        //    Assert.IsFalse(result1);

        //    // Act
        //    var result2 = await userManager.ConfirmEmailAsync(user, token);

        //    // Assert
        //    Assert.IsTrue(result2.Succeeded);
        //    var result3 = await userManager.IsEmailConfirmedAsync(user);
        //    Assert.IsTrue(result3);
        //}


        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task IsEmailConfirmedAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);
        //    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        //    var result1 = await userManager.IsEmailConfirmedAsync(user);
        //    Assert.IsFalse(result1);
        //    var result2 = await userManager.ConfirmEmailAsync(user, token);

        //    // Act
        //    var result3 = await userManager.IsEmailConfirmedAsync(user);

        //    // Assert
        //    Assert.IsTrue(result3);
        //}

        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task GenerateChangeEmailTokenAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);
        //    var id = user.Id;
        //    var result1 = await userManager.IsEmailConfirmedAsync(user);
        //    var emailAddress = $"Ty{user.Email}";
        //    Assert.IsFalse(result1);

        //    // Act
        //    var token = await userManager.GenerateChangeEmailTokenAsync(user, emailAddress);

        //    // Assert
        //    var result2 = await userManager.ChangeEmailAsync(user, emailAddress, token);
        //    var result3 = await userManager.FindByEmailAsync(emailAddress);
        //    Assert.AreEqual(id, result3.Id);
        //    var result4 = await userManager.IsEmailConfirmedAsync(user);
        //    Assert.IsTrue(result4);
        //}

        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task ChangeEmailAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));

        //    var user = await GetTestUser(userManager);
        //    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        //    var emailAddress = "Bb" + user.Email;

        //    // Act
        //    var result1 = await userManager.ChangeEmailAsync(user, emailAddress, token);

        //    // Assert
        //    Assert.IsTrue(result1.Succeeded);
        //    var result2 = await userManager.GetEmailAsync(user);
        //    Assert.AreEqual(emailAddress, result2);
        //    var result3 = await userManager.FindByIdAsync(user.Id);
        //    Assert.AreEqual(emailAddress.ToLowerInvariant(), result3.NormalizedEmail);

        //}

        [TestMethod]
        public async Task GetPhoneNumberAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var phoneNumber = "3334445555";
            var result1 = await userManager.SetPhoneNumberAsync(user, phoneNumber);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.GetPhoneNumberAsync(user);

            // Assert
            Assert.AreEqual(phoneNumber, result2);
        }

        [TestMethod]
        public async Task SetPhoneNumberAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var phoneNumber = "3334445555";

            // Act
            var result1 = await userManager.SetPhoneNumberAsync(user, phoneNumber);

            // Act
            Assert.IsTrue(result1.Succeeded);
            var result2 = await userManager.GetPhoneNumberAsync(user);
            Assert.AreEqual(phoneNumber, result2);
        }


        // TODO: Register two factor token provider in order for this to work
        //[TestMethod]
        //public async Task ChangePhoneNumberAsyncTest()
        //{
        //    // Arrange
        //    using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
        //    var user = await GetTestUser(userManager);
        //    var phoneNumber1 = "3334445555";
        //    var phoneNumber2 = "1114445555";
        //    var token = await userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber2);
        //    var result1 = await userManager.SetPhoneNumberAsync(user, phoneNumber1);
        //    Assert.IsTrue(result1.Succeeded);
        //    var result2 = await userManager.GetPhoneNumberAsync(user);
        //    Assert.AreEqual(phoneNumber1, result2);

        //    // Act
        //    var result3 = await userManager.ChangePhoneNumberAsync(user, phoneNumber2, token);
        //}

        [TestMethod]
        public async Task IsPhoneNumberConfirmedAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act

        }

        [TestMethod]
        public async Task GenerateChangePhoneNumberTokenAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act

        }

        [TestMethod]
        public async Task VerifyChangePhoneNumberTokenAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act

        }

        //[TestMethod]
        //public async Task VerifyUserTokenAsyncTest()
        //{
        //}

        //[TestMethod]
        //public async Task GenerateUserTokenAsyncTest()
        //{
        //}

        //[TestMethod]
        //public async Task RegisterTokenProviderTest()
        //{

        //}

        //[TestMethod]
        //public async Task GetValidTwoFactorProvidersAsyncTest()
        //{
        //}

        //[TestMethod]
        //public async Task VerifyTwoFactorTokenAsyncTest()
        //{
        //}

        //[TestMethod]
        //public async Task GenerateTwoFactorTokenAsyncTest()
        //{
        //}

        [TestMethod]
        public async Task GetTwoFactorEnabledAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result1 = await userManager.GetTwoFactorEnabledAsync(user);

            // Assert
            Assert.IsFalse(result1);
        }

        [TestMethod]
        public async Task SetTwoFactorEnabledAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.SetTwoFactorEnabledAsync(user, true);

            // Assert
            Assert.IsTrue(result.Succeeded);
            var result2 = await userManager.GetTwoFactorEnabledAsync(user);
            Assert.IsTrue(result2);


        }

        [TestMethod]
        public async Task IsLockedOutAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result = await userManager.IsLockedOutAsync(user);

            // Assert
            Assert.IsFalse(false);
        }

        [TestMethod]
        public async Task SetLockoutEnabledAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result1 = await userManager.SetLockoutEnabledAsync(user, true);

            // Assert
            Assert.IsTrue(result1.Succeeded);
            var result2 = await userManager.GetLockoutEnabledAsync(user);
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public async Task GetLockoutEnabledAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var result1 = await userManager.SetLockoutEnabledAsync(user, true);
            Assert.IsTrue(result1.Succeeded);

            // Act
            var result2 = await userManager.GetLockoutEnabledAsync(user);

            // Assert
            Assert.IsTrue(result2);
        }

        [TestMethod]
        public async Task GetLockoutEndDateAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act

        }

        [TestMethod]
        public async Task SetLockoutEndDateAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var dateTime = DateTimeOffset.Now.AddMinutes(15);

            // Act
            var result1 = await userManager.SetLockoutEndDateAsync(user, dateTime);

            // Assert
            Assert.IsTrue(result1.Succeeded);
            var result2 = await userManager.GetLockoutEndDateAsync(user);
            Assert.AreEqual(dateTime, result2);
        }

        [TestMethod]
        public async Task AccessFailedAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);

            // Act
            var result1 = await userManager.AccessFailedAsync(user);
            var result2 = await userManager.AccessFailedAsync(user);

            // Assert
            var result3 = await userManager.GetAccessFailedCountAsync(user);
            Assert.AreEqual(2, result3);
        }

        [TestMethod]
        public async Task ResetAccessFailedCountAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            var user = await GetTestUser(userManager);
            var result1 = await userManager.AccessFailedAsync(user);
            var result2 = await userManager.AccessFailedAsync(user);
            var result3 = await userManager.GetAccessFailedCountAsync(user);
            Assert.AreEqual(2, result3);

            // Act
            var result4 = await userManager.ResetAccessFailedCountAsync(user);

            // Assert
            Assert.IsTrue(result4.Succeeded);
            var result5 = await userManager.GetAccessFailedCountAsync(user);
            Assert.AreEqual(0, result5);

        }

        [TestMethod]
        public async Task GetUsersInRoleAsyncTest()
        {
            // Arrange
            using var userManager = GetTestUserManager(_testUtilities.GetUserStore(connectionString, databaseName));
            using var roleManager = GetTestRoleManager(_testUtilities.GetRoleStore(connectionString, databaseName));
            var user1 = await GetTestUser(userManager);
            var user2 = await GetTestUser(userManager);
            var user3 = await GetTestUser(userManager);
            var role = await GetMockRandomRoleAsync(null, false);
            await roleManager.CreateAsync(role);
            var result1 = await userManager.AddToRoleAsync(user1, role.Name);
            Assert.IsTrue(result1.Succeeded);
            var result2 = await userManager.AddToRoleAsync(user2, role.Name);
            Assert.IsTrue(result2.Succeeded);
            var result3 = await userManager.AddToRoleAsync(user3, role.Name);
            Assert.IsTrue(result3.Succeeded);

            // Act
            var result4 = await userManager.GetUsersInRoleAsync(role.Name);

            // Assert
            Assert.AreEqual(3, result4.Count());

        }

    }
}
