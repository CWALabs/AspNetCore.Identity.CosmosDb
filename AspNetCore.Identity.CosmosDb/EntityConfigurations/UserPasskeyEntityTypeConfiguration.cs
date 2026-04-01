using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Text.Json;

namespace AspNetCore.Identity.CosmosDb.EntityConfigurations
{
    public class UserPasskeyEntityTypeConfiguration<TKey> : IEntityTypeConfiguration<IdentityUserPasskey<TKey>>
        where TKey : IEquatable<TKey>
    {
        private readonly string _tableName;

        public UserPasskeyEntityTypeConfiguration(string tableName = "Identity_Passkeys")
        {
            _tableName = tableName;
        }

        public void Configure(EntityTypeBuilder<IdentityUserPasskey<TKey>> builder)
        {
            builder.HasKey(_ => _.CredentialId);

            builder
                .UseETagConcurrency()
                .HasPartitionKey(_ => _.UserId);

            builder.Property(_ => _.CredentialId).HasMaxLength(1024);
            builder.Property(_ => _.Data)
                .IsRequired()
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<IdentityPasskeyData>(v, (JsonSerializerOptions)null));

            builder.ToContainer(_tableName);
        }
    }
}
