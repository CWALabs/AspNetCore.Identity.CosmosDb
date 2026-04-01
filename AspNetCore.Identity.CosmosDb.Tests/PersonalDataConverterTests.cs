using Microsoft.AspNetCore.Identity;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class PersonalDataConverterTests
    {
        [TestMethod]
        public void Converter_RoundTripsProtectedValue()
        {
            var converter = new PersonalDataConverter(new TestProtector());

            var toProvider = converter.ConvertToProviderExpression.Compile();
            var fromProvider = converter.ConvertFromProviderExpression.Compile();

            var original = "sensitive-value";
            var protectedValue = toProvider(original);
            var roundTrip = fromProvider(protectedValue);

            Assert.AreEqual("enc:sensitive-value", protectedValue);
            Assert.AreEqual(original, roundTrip);
        }

        private sealed class TestProtector : IPersonalDataProtector
        {
            public string Protect(string data)
            {
                return $"enc:{data}";
            }

            public string Unprotect(string data)
            {
                return data.Replace("enc:", string.Empty);
            }
        }
    }
}
