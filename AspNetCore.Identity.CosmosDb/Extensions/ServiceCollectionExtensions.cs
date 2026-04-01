using AspNetCore.Identity.CosmosDb.Contracts;
using AspNetCore.Identity.CosmosDb.Repositories;
using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace AspNetCore.Identity.CosmosDb.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the default identity system configuration for the specified User and Role types.
        /// </summary>
        /// <typeparam name="TUser">The type representing a User in the system.</typeparam>
        /// <typeparam name="TRole">The type representing a Role in the system.</typeparam>
        /// <param name="services">The services available in the application.</param>
        /// <returns>An <see cref="IdentityBuilder"/> for creating and configuring the identity system.</returns>
        public static IdentityBuilder AddCosmosIdentity<TUser, TRole, TKey>(
            this IServiceCollection services)
            where TUser : IdentityUser<TKey>, new()
            where TRole : class, new()
            where TKey : IEquatable<TKey>
            => services.AddIdentity<TUser, TRole>(setupAction: null!);

        /// <summary>
        /// Adds and configures the identity system for the specified User and Role types, using Cosmos DB as the data store.
        /// </summary>
        /// <typeparam name="TUser">The type representing a User in the system.</typeparam>
        /// <typeparam name="TRole">The type representing a Role in the system.</typeparam>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="IdentityOptions"/>.</param>
        /// <returns>An <see cref="IdentityBuilder"/> for creating and configuring the identity system.</returns>
        /// <remarks>
        /// This class is based on the <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/IdentityServiceCollectionExtensions.cs">AddIdentity()</see>.
        /// </remarks>
        public static IdentityBuilder AddCosmosIdentity<TDbContext, TUser, TRole, TKey>(
            this IServiceCollection services,
            Action<IdentityOptions> setupAction
        )
            where TDbContext : CosmosIdentityDbContext<TUser, TRole, TKey>
            where TUser : IdentityUser<TKey>, new()
            where TRole : IdentityRole<TKey>, new()
            where TKey : IEquatable<TKey>
        {
            return ServiceCollectionExtensions.AddCosmosIdentity<TDbContext, TUser, TRole, TKey>(services, setupAction, TimeSpan.FromMinutes(5), slidingExpiration: false);
        }
        /// <summary>
        /// Adds and configures the identity system for the specified User and Role types, using Cosmos DB as the data store.
        /// </summary>
        /// <typeparam name="TUser">The type representing a User in the system.</typeparam>
        /// <typeparam name="TRole">The type representing a Role in the system.</typeparam>
        /// <param name="services">The services available in the application.</param>
        /// <param name="setupAction">An action to configure the <see cref="IdentityOptions"/>.</param>
        /// <param name="cookieExpireTimeSpan">How long to allow the authentication cookie to exist before expiring.</param>
        /// <param name="slidingExpiration">Whether accessing the site and using the cookie will extend the expiration of the authentication cookie.</param>
        /// <returns>An <see cref="IdentityBuilder"/> for creating and configuring the identity system.</returns>
        /// <remarks>
        /// This class is based on the <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/IdentityServiceCollectionExtensions.cs">AddIdentity()</see>.
        /// </remarks>
        public static IdentityBuilder AddCosmosIdentity<TDbContext, TUser, TRole, TKey>(
            this IServiceCollection services,
            Action<IdentityOptions> setupAction,
            TimeSpan cookieExpireTimeSpan,
            bool slidingExpiration = false
        )
            where TDbContext : CosmosIdentityDbContext<TUser, TRole, TKey>
            where TUser : IdentityUser<TKey>, new()
            where TRole : IdentityRole<TKey>, new()
            where TKey : IEquatable<TKey>
        {
            //services.AddAuthentication().AddCookie(IdentityConstants.ExternalScheme).AddApplicationCookie();
            //services.TryAddSingleton<ISystemClock, SystemClock>();

            // Services used by identity
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
                .AddCookie(IdentityConstants.ApplicationScheme, o =>
                {
                    o.LoginPath = new PathString("/Account/Login");
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync
                    };
                    o.ExpireTimeSpan = cookieExpireTimeSpan;
                    o.SlidingExpiration = slidingExpiration;
                })
                .AddCookie(IdentityConstants.ExternalScheme, o =>
                {
                    o.Cookie.Name = IdentityConstants.ExternalScheme;
                    o.ExpireTimeSpan = cookieExpireTimeSpan;
                    o.SlidingExpiration = slidingExpiration;
                })
                .AddCookie(IdentityConstants.TwoFactorRememberMeScheme, o =>
                {
                    o.Cookie.Name = IdentityConstants.TwoFactorRememberMeScheme;
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = SecurityStampValidator.ValidateAsync<ITwoFactorSecurityStampValidator>
                    };
                })
                .AddCookie(IdentityConstants.TwoFactorUserIdScheme, o =>
                {
                    o.Cookie.Name = IdentityConstants.TwoFactorUserIdScheme;
                    o.ExpireTimeSpan = cookieExpireTimeSpan;
                    o.SlidingExpiration = slidingExpiration;
                });

            // Hosting doesn't add IHttpContextAccessor by default
            services.AddHttpContextAccessor();

            // Add repository service (Connects to Cosmos DB)
            services.AddTransient<IRepository, CosmosIdentityRepository<TDbContext, TUser, TRole, TKey>>();

            // Data stores
            services.TryAddScoped<IUserStore<TUser>, CosmosUserStore<TUser, TRole, TKey>>();
            services.TryAddScoped<IRoleStore<TRole>, CosmosRoleStore<TRole, TKey>>();

            // Identity services
            services.TryAddScoped<IUserValidator<TUser>, UserValidator<TUser>>();
            services.TryAddScoped<IPasswordValidator<TUser>, PasswordValidator<TUser>>();
            services.TryAddScoped<IPasswordHasher<TUser>, PasswordHasher<TUser>>();
            services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            services.TryAddScoped<IRoleValidator<TRole>, RoleValidator<TRole>>();
            // No interface for the error describer so we can add errors without rev'ing the interface
            services.TryAddScoped<IdentityErrorDescriber>();
            services.TryAddScoped<ISecurityStampValidator, SecurityStampValidator<TUser>>();
            services.TryAddScoped<ITwoFactorSecurityStampValidator, TwoFactorSecurityStampValidator<TUser>>();
            services.TryAddScoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser, TRole>>();
            services.TryAddScoped<IUserConfirmation<TUser>, DefaultUserConfirmation<TUser>>();
            services.TryAddScoped<UserManager<TUser>>();
            services.TryAddScoped<SignInManager<TUser>>();
            services.TryAddScoped<RoleManager<TRole>>();

            if (setupAction != null)
            {
                services.Configure(setupAction);
            }

            return new IdentityBuilder(typeof(TUser), typeof(TRole), services);
        }
    }
}