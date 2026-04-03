using AspNetCore.Identity.CosmosDb.Containers;
using Microsoft.Azure.Cosmos;
using System.Net;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9.Containers
{
    [TestClass()]
    [DoNotParallelize]
    public class ContainerUtilitiesTests
    {

        private static TestUtilities utils;
        private static ContainerUtilities containerUtilities;
        private static string testDatabaseName;

        /// <summary>
        /// Class initialize
        /// </summary>
        /// <param name="context"></param>
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            //
            // Setup context.
            //
            utils = new TestUtilities();
            testDatabaseName = $"{TestUtilities.GetKeyValue("CosmosIdentityDbName")}-cu-{Guid.NewGuid():N}";
            containerUtilities = utils.GetContainerUtilities(TestUtilities.GetKeyValue("ApplicationDbContextConnection"), testDatabaseName);
        }

        /// <summary>
        /// Class cleanup
        /// </summary>
        /// <param name="context"></param>
        [ClassCleanup]
        public static void ClassCleanup()
        {
            containerUtilities.DeleteDatabaseIfExists(testDatabaseName).GetAwaiter().GetResult();
            containerUtilities.Dispose();
        }

        /// <summary>
        /// Removes all containers prior to running tests.
        /// </summary>
        /// <returns></returns>
        //[TestMethod()]
        //public async Task A1_RemoveAllContainersPriorToTest()
        //{
        //    Assert.IsNotNull(containerUtilities);

        //    // Get rid of all the containers if they exist.
        //    await containerUtilities.DeleteRequiredContainers();
        //}

        [TestMethod()]
        public async Task A1_DeleteDatabaseIfExistsTest()
        {
            var result = await containerUtilities.DeleteDatabaseIfExists(testDatabaseName);

            Assert.IsTrue(result == null || result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.NoContent);
        }

        [TestMethod()]
        public async Task A2_CreateDatabaseIfExistsTest()
        {
            var result = await containerUtilities.CreateDatabaseAsync(testDatabaseName);

            Assert.IsTrue(result.StatusCode == System.Net.HttpStatusCode.OK || result.StatusCode == System.Net.HttpStatusCode.NoContent || result.StatusCode == System.Net.HttpStatusCode.Created);
        }

        ///// <summary>
        ///// Establishes the utilities class can be created.
        ///// </summary>
        //[TestMethod()]
        //public void ContainerUtilitiesTest()
        //{
        //    Assert.IsNotNull(containerUtilities);
        //}

        [TestMethod()]
        public async Task A3_CreateRequiredContainersTest()
        {
            try
            {
                await containerUtilities.CreateDatabaseAsync(testDatabaseName);

                var containers = await containerUtilities.CreateRequiredContainers();

                var requiredContainerDefinitions = containerUtilities.GetRequiredContainerDefinitions();

                Assert.AreEqual(requiredContainerDefinitions.Count, containers.Count);

                foreach (var con in requiredContainerDefinitions)
                {
                    Assert.IsTrue(containers.Any(a => a.Id == con.ContainerName));
                }
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Forbidden && ex.SubStatusCode == 3090)
            {
                Assert.Inconclusive("Skipped due to Cosmos DB collection quota exhaustion in the shared CI account (403/3090).");
            }
        }

        [TestMethod()]
        public async Task A4_DeleteContainerIfExists_WhenContainerMissing_ReturnsTrue()
        {
            await containerUtilities.CreateDatabaseAsync(testDatabaseName);

            var result = await containerUtilities.DeleteContainerIfExists($"missing-{Guid.NewGuid():N}");

            Assert.IsTrue(result);
        }

        [TestMethod()]
        public async Task A5_CreateContainerIfNotExistsAsync_InvalidPartitionKey_Throws()
        {
            await containerUtilities.CreateDatabaseAsync(testDatabaseName);

            await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
                await containerUtilities.CreateContainerIfNotExistsAsync($"badpk-{Guid.NewGuid():N}", "UserId"));
        }

        [TestMethod()]
        public void A6_GetRequiredContainerDefinitions_IncludesPasskeys()
        {
            var required = containerUtilities.GetRequiredContainerDefinitions();

            Assert.IsTrue(required.Any(_ => _.ContainerName == "Identity_Passkeys" && _.PartitionKey == "/UserId"));
        }

        [TestMethod()]
        public void A7_Constructor_WithInvalidArguments_Throws()
        {
            var connectionString = TestUtilities.GetKeyValue("ApplicationDbContextConnection");

            Assert.ThrowsExactly<ArgumentNullException>(() => new ContainerUtilities(string.Empty, testDatabaseName));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ContainerUtilities(connectionString, string.Empty));
        }

        [TestMethod()]
        public async Task A8_DeleteRequiredContainers_DoesNotThrow_WhenContainersExist()
        {
            try
            {
                await containerUtilities.CreateDatabaseAsync(testDatabaseName);
                await containerUtilities.CreateRequiredContainers();

                await containerUtilities.DeleteRequiredContainers();
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Forbidden && ex.SubStatusCode == 3090)
            {
                Assert.Inconclusive("Skipped due to Cosmos DB collection quota exhaustion in the shared CI account (403/3090).");
            }
        }

        [TestMethod()]
        public async Task A9_ArgumentGuards_ThrowExpectedExceptions()
        {
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await containerUtilities.CreateDatabaseAsync(string.Empty));

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await containerUtilities.DeleteDatabaseIfExists(string.Empty));

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await containerUtilities.CreateContainerIfNotExistsAsync(string.Empty, "/Id"));

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await containerUtilities.CreateContainerIfNotExistsAsync("abc", string.Empty));

            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
                await containerUtilities.DeleteContainerIfExists(string.Empty));
        }
    }
}