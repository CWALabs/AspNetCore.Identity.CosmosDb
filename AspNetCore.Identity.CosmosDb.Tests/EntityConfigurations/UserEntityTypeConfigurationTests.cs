using AspNetCore.Identity.CosmosDb.EntityConfigurations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9.EntityConfigurations
{
    [TestClass]
    public class UserEntityTypeConfigurationTests
    {
        [TestMethod]
        public void Configure_WithProtectedStringProperty_ConfiguresConverter()
        {
            var builder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
            var converter = new PersonalDataConverter(new TestProtector());
            var config = new UserEntityTypeConfiguration<ProtectedStringUser, string>(converter);

            config.Configure(builder.Entity<ProtectedStringUser>());

            var entity = builder.Model.FindEntityType(typeof(ProtectedStringUser));
            var property = entity?.FindProperty(nameof(ProtectedStringUser.Secret));

            Assert.IsNotNull(property);
            Assert.IsNotNull(property.GetValueConverter());
        }

        [TestMethod]
        public void Configure_WithProtectedNonStringProperty_ThrowsInvalidOperationException()
        {
            var builder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
            var converter = new PersonalDataConverter(new TestProtector());
            var config = new UserEntityTypeConfiguration<ProtectedNonStringUser, string>(converter);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                config.Configure(builder.Entity<ProtectedNonStringUser>()));
        }

        private sealed class ProtectedStringUser : IdentityUser<string>
        {
            [ProtectedPersonalData]
            public string Secret { get; set; } = string.Empty;
        }

        private sealed class ProtectedNonStringUser : IdentityUser<string>
        {
            [ProtectedPersonalData]
            public int Secret { get; set; }
        }

        private sealed class TestProtector : IPersonalDataProtector
        {
            public string Protect(string data) => $"enc:{data}";

            public string Unprotect(string data) => data.Replace("enc:", string.Empty);
        }
    }
}
