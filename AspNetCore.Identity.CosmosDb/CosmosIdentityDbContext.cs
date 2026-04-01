using AspNetCore.Identity.CosmosDb.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace AspNetCore.Identity.CosmosDb
{
    /// <summary>
    /// Entity Framework Core Cosmos DB context used by the ASP.NET Core Identity stores in this package.
    /// </summary>
    /// <typeparam name="TUser">The user entity type.</typeparam>
    /// <typeparam name="TRole">The role entity type.</typeparam>
    /// <typeparam name="TKey">The primary key type for users and roles.</typeparam>
    public class CosmosIdentityDbContext<TUser, TRole, TKey> :
        IdentityDbContext<TUser, TRole, TKey>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
    {

        private readonly bool _backwardCompatibility;

        private StoreOptions? GetStoreOptions() => this.GetService<IDbContextOptions>()
                        .Extensions.OfType<CoreOptionsExtension>()
                        .FirstOrDefault()?.ApplicationServiceProvider
                        ?.GetService<IOptions<IdentityOptions>>()
                        ?.Value?.Stores;

        /// <summary>
        /// Initializes a Cosmos-backed Identity DbContext.
        /// </summary>
        /// <param name="options">The EF Core DbContext options.</param>
        /// <param name="backwardCompatibility">
        /// When <see langword="true"/>, configures the model to read older databases that use
        /// <c>Discriminator</c> instead of <c>$type</c> for the embedded discriminator name.
        /// </param>
        public CosmosIdentityDbContext(
            DbContextOptions options,
            bool backwardCompatibility = false)
            : base(options)
        {
            _backwardCompatibility = backwardCompatibility;
        }

        private int GetMaxKeyLength(StoreOptions? storeOptions)
        {
            var maxKeyLength = storeOptions?.MaxLengthForKeys ?? 0;
            return maxKeyLength == 0 ? 128 : maxKeyLength;
        }

        private PersonalDataConverter? CreatePersonalDataConverter(StoreOptions? storeOptions)
        {
            if (storeOptions?.ProtectPersonalData != true)
            {
                return null;
            }

            return new PersonalDataConverter(this.GetService<IPersonalDataProtector>());
        }

        private static void ConfigureCosmosIdentityConventions(ModelBuilder builder)
        {
            // dotnet/efcore#35224
            // New behavior for Cosmos DB EF is new. For backward compatibility,
            // we need to add the following line to the OnModelCreating method.
            builder.HasDiscriminatorInJsonIds();

            // dotnet/efcore#35264
            // Cosmos DB EF now throws when indexes are detected. That means we must
            // not call the IdentityDbContext base implementation here.
#pragma warning disable S125 // Sections of code should not be commented out
            // base.OnModelCreating(builder);
#pragma warning restore S125 // Sections of code should not be commented out
        }

        private void ConfigureBackwardCompatibility(ModelBuilder builder)
        {
            if (_backwardCompatibility)
            {
                builder.HasEmbeddedDiscriminatorName("Discriminator");
            }
        }

        /// <summary>
        /// Configures Cosmos-specific Identity model conventions.
        /// </summary>
        /// <param name="builder">The model builder.</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            ConfigureCosmosIdentityConventions(builder);

            var storeOptions = GetStoreOptions();
            var maxKeyLength = GetMaxKeyLength(storeOptions);
            var dataConverter = CreatePersonalDataConverter(storeOptions);

            // Cosmos DB Modifications
            builder.ApplyIdentityMappings<TUser, TRole, TKey>(dataConverter, maxKeyLength);

            ConfigureBackwardCompatibility(builder);
        }

    }
}