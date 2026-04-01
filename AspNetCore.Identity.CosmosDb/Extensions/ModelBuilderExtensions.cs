using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AspNetCore.Identity.CosmosDb.EntityConfigurations;

namespace AspNetCore.Identity.CosmosDb.Extensions
{
    public static class ModelBuilderExtensions
    {
        public static ModelBuilder ApplyIdentityMappings<TUserEntity, TRoleEntity, TKey>(this ModelBuilder builder, PersonalDataConverter? dataConverter, int maxKeyLength)
            where TUserEntity : IdentityUser<TKey>
            where TRoleEntity : IdentityRole<TKey>
            where TKey : IEquatable<TKey>
        {
            builder.ApplyConfiguration(new UserEntityTypeConfiguration<TUserEntity, TKey>(dataConverter) { });
            builder.ApplyConfiguration(new UserRoleEntityTypeConfiguration<TKey> { });
            builder.ApplyConfiguration(new RoleEntityTypeConfiguration<TRoleEntity, TKey> { });
            builder.ApplyConfiguration(new RoleClaimEntityTypeConfiguration<TKey> { });
            builder.ApplyConfiguration(new UserClaimEntityTypeConfiguration<TKey> { });
            builder.ApplyConfiguration(new UserLoginEntityTypeConfiguration<TKey>(maxKeyLength) { });
            builder.ApplyConfiguration(new UserTokensEntityTypeConfiguration<TKey>(maxKeyLength) { });
            builder.ApplyConfiguration(new UserPasskeyEntityTypeConfiguration<TKey> { });
            // The following may required a license for production.
            // See: https://modlogix.com/blog/identityserver4-alternatives-best-options-and-the-near-future-of-identityserver/
            builder.ApplyConfiguration(new DeviceFlowCodesEntityTypeConfiguration { });
            builder.ApplyConfiguration(new PersistedGrantEntityTypeConfiguration { });

            return builder;
        }
    }
}