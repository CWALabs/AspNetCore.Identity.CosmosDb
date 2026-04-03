using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    public abstract class CosmosIdentityTestsBase
    {
        protected static TestUtilities _testUtilities;
        protected static Random _random;

        protected static void InitializeClass(string connectionString, string databaseName, bool backwardCompatibility = false)
        {
            //
            // Setup context.
            //
            _testUtilities = new TestUtilities();
            _random = new Random();

            _testUtilities
                .EnsureIdentityInfrastructureAsync(connectionString, databaseName)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// Gets a random number
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        protected int GetNextRandomNumber(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Gets a mock <see cref="IdentityRole"/> for unit testing purposes
        /// </summary>
        /// <returns></returns>
        protected async Task<IdentityRole> GetMockRandomRoleAsync(
            CosmosRoleStore<IdentityRole, string> roleStore, bool saveToDatabase = true)
        {
            var role = new IdentityRole($"role-{Guid.NewGuid():N}");
            role.NormalizedName = role.Name.ToUpper();

            if (roleStore != null && saveToDatabase)
            {
                var result = await roleStore.CreateAsync(role);
                role = await roleStore.FindByIdAsync(role.Id);
                Assert.IsTrue(result.Succeeded); //Confirm success
            }

            return role;
        }

        /// <summary>
        /// Gets a mock <see cref="IdentityUser"/> for unit testing purposes
        /// </summary>
        /// <returns></returns>
        protected async Task<IdentityUser> GetMockRandomUserAsync(
            CosmosUserStore<IdentityUser, IdentityRole, string> userStore, bool saveToDatabase = true)
        {
            var randomEmail = $"{GetNextRandomNumber(1000, 9999)}@{GetNextRandomNumber(10000, 99999)}.com";
            var user = new IdentityUser(randomEmail) { Email = randomEmail, Id = Guid.NewGuid().ToString() };

            user.NormalizedUserName = user.UserName.ToUpper();
            user.NormalizedEmail = user.Email.ToUpper();

            if (userStore != null && saveToDatabase)
            {
                var result = await userStore.CreateAsync(user);
                Assert.IsTrue(result.Succeeded); //Confirm success
                user = await userStore.FindByNameAsync(user.UserName.ToUpper());
            }

            return user;
        }

        /// <summary>
        /// Gets a mock login info for testing purposes
        /// </summary>
        /// <returns></returns>
        protected UserLoginInfo GetMockLoginInfoAsync()
        {
            return new UserLoginInfo("Twitter", Guid.NewGuid().ToString(), "Twitter");
        }

        protected Claim GetMockClaim(string seed = "")
        {
            return new Claim(Guid.NewGuid().ToString(), $"{Guid.NewGuid().ToString()}{seed}");
        }

        /// <summary>
        /// Gets a user manager for testing purposes
        /// </summary>
        /// <typeparam name="TUser"></typeparam>
        /// <param name="store"></param>
        /// <returns></returns>
        public UserManager<TUser> GetTestUserManager<TUser>(IUserStore<TUser> store)
            where TUser : class
        {
            var builder = new IdentityBuilder(typeof(IdentityUser), new ServiceCollection());

            var userType = builder.UserType;

            var dataProtectionProviderType = typeof(DataProtectorTokenProvider<>).MakeGenericType(userType);
            var phoneNumberProviderType = typeof(PhoneNumberTokenProvider<>).MakeGenericType(userType);
            var emailTokenProviderType = typeof(EmailTokenProvider<>).MakeGenericType(userType);
            var authenticatorProviderType = typeof(AuthenticatorTokenProvider<>).MakeGenericType(userType);
            //var authenticatorProviderType = typeof(UserTwoFactorTokenProvider<>).MakeGenericType(userType);


            store = store ?? new Mock<IUserStore<TUser>>().Object;
            var options = new Mock<IOptions<IdentityOptions>>();
            var idOptions = new IdentityOptions();

            options.Setup(o => o.Value).Returns(idOptions);
            var userValidators = new List<IUserValidator<TUser>>();
            var validator = new Mock<IUserValidator<TUser>>();
            userValidators.Add(validator.Object);
            var pwdValidators = new List<PasswordValidator<TUser>>();
            pwdValidators.Add(new PasswordValidator<TUser>());
            var userManager = new UserManager<TUser>(store, options.Object, new PasswordHasher<TUser>(),
                userValidators, pwdValidators, MockLookupNormalizer(),
                new IdentityErrorDescriber(), null,
                new Mock<ILogger<UserManager<TUser>>>().Object);
            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<TUser>()))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();

            return userManager;
        }

        public RoleManager<TRole> GetTestRoleManager<TRole>(IRoleStore<TRole> store)
            where TRole : class
        {
            store = store ?? new Mock<IRoleStore<TRole>>().Object;
            var roles = new List<IRoleValidator<TRole>>();
            roles.Add(new RoleValidator<TRole>());
            var roleManager = new RoleManager<TRole>(store, roles, MockLookupNormalizer(),
                new IdentityErrorDescriber(), new Mock<ILogger<RoleManager<TRole>>>().Object);
            return roleManager;
        }

        public ILookupNormalizer MockLookupNormalizer()
        {
            var normalizerFunc = new Func<string, string>(i =>
            {
                if (i == null)
                {
                    return null;
                }
                else
                {
                    return i.ToUpperInvariant();
                }
            });
            var lookupNormalizer = new Mock<ILookupNormalizer>();
            lookupNormalizer.Setup(i => i.NormalizeName(It.IsAny<string>())).Returns(normalizerFunc);
            lookupNormalizer.Setup(i => i.NormalizeEmail(It.IsAny<string>())).Returns(normalizerFunc);
            return lookupNormalizer.Object;
        }

        public SignInManager<TUser> GetTestSignInManager<TUser>(
            UserManager<TUser> userManager)
            where TUser : class
        {
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<TUser>>();
            var options = new Mock<IOptions<IdentityOptions>>();
            var logger = new Mock<ILogger<SignInManager<TUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var confirmation = new Mock<IUserConfirmation<TUser>>();

            options.Setup(o => o.Value).Returns(new IdentityOptions());

            var signInManager = new SignInManager<TUser>(
                userManager,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                options.Object,
                logger.Object,
                schemes.Object,
                confirmation.Object);

            return signInManager;
        }

        /// <summary>
        /// Helper method to create a UserPasskeyInfo for testing
        /// </summary>
        protected UserPasskeyInfo CreatePasskeyInfo(
            string name,
            uint signCount = 0,
            bool isUserVerified = false,
            bool isBackupEligible = false,
            bool isBackedUp = false)
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
                isUserVerified: isUserVerified,
                isBackupEligible: isBackupEligible,
                isBackedUp: isBackedUp,
                attestationObject,
                clientDataJson)
            {
                Name = name
            };
        }
    }
}