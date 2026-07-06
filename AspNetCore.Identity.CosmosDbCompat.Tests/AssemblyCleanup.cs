using AspNetCore.Identity.CosmosDb.Tests.Net9;

namespace AspNetCore.Identity.CosmosDb.Tests.Net7
{
    [TestClass]
    public static class AssemblyCleanup
    {
        [AssemblyCleanup]
        public static async Task CleanupAsync()
        {
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");
            await TestUtilities.CleanupRegisteredDatabasesAsync(connectionString);
        }
    }
}
