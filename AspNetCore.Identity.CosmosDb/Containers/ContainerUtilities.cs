using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Containers
{
    /// <summary>
    /// Utilities for creating Cosmos DB Containers
    /// </summary>
    /// <remarks>
    /// This class is only meant to run when the database needs to be created, deleted or containers removed without using the DbContext.
    /// A better approach is to use DbContext.EnsureCreated.
    /// </remarks>
    public class ContainerUtilities : IDisposable
    {
        private readonly CosmosClient _client;
        private readonly string _databaseName;

        /// <summary>
        /// Constructor that creates the database if it does not already exist.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="databaseName"></param>
        /// <param name="limitThroughput"></param>
        /// <param name="clientOptions"></param>
        public ContainerUtilities(string connectionString,
                                  string databaseName,
                                  CosmosClientOptions? clientOptions = null)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            _client = new CosmosClient(connectionString, clientOptions);
            _databaseName = databaseName;
        }

        /// <summary>
        /// Create a Cosmos database
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        public async Task<DatabaseResponse> CreateDatabaseAsync(string databaseName)
        {
            var result = await _client.CreateDatabaseIfNotExistsAsync(id: databaseName);
            return result;
        }

        /// <summary>
        /// Create all required containers with their partition key path
        /// </summary>
        /// <returns></returns>
        public async Task<List<Container>> CreateRequiredContainers()
        {
            var containers = GetRequiredContainerDefinitions();

            List<Container> containerList = new List<Container>();

            foreach (var container in containers)
            {
                containerList.Add(await CreateContainerIfNotExistsAsync(container.ContainerName, container.PartitionKey));
            }

            return containerList;
        }

        public async Task DeleteRequiredContainers()
        {
            var containers = GetRequiredContainerDefinitions();

            foreach (var container in containers)
            {
                _ = await DeleteContainerIfExists(container.ContainerName);
            }
        }

        /// <summary>
        /// Deletes the database.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <remarks>WARNING! ALL DATA WILL BE LOST AND THIS CANNOT BE UNDONE!</remarks>
        public async Task<DatabaseResponse?> DeleteDatabaseIfExists(string databaseName)
        {
            if (string.IsNullOrEmpty(databaseName))
                throw new ArgumentNullException(nameof(databaseName));

            var database = _client.GetDatabase(databaseName);

            try
            {
                var response = await database.DeleteAsync();

                return response;
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates the specified container if it does not already exist.
        /// </summary>
        /// <param name="containerName"></param>
        /// <param name="partitionKeyPath"></param>
        /// <param name="throughput"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Container> CreateContainerIfNotExistsAsync(string containerName, string partitionKeyPath, int? throughput = null)
        {
            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName));

            if (string.IsNullOrEmpty(partitionKeyPath))
                throw new ArgumentNullException(nameof(partitionKeyPath));

            if (!partitionKeyPath.StartsWith('/'))
                throw new ArgumentException(nameof(partitionKeyPath), "Path must begin with /");

            Container container = await _client.GetDatabase(_databaseName).CreateContainerIfNotExistsAsync(
                    id: containerName,
                    partitionKeyPath: partitionKeyPath, throughput);
            return container;
        }

        /// <summary>
        /// Deletes a container
        /// </summary>
        /// <param name="containerName"></param>
        /// <returns>success</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<bool> DeleteContainerIfExists(string containerName)
        {
            if (string.IsNullOrEmpty(containerName))
                throw new ArgumentNullException(nameof(containerName));

            var database = _client.GetDatabase(_databaseName);
            var container = database.GetContainer(containerName);

            ContainerResponse? response = null;
            try
            {
                response = await container.DeleteContainerAsync();
            }
            catch (CosmosException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return true;
            }

            switch (response.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                case System.Net.HttpStatusCode.NoContent:
                    return true;
                case System.Net.HttpStatusCode.Unauthorized:
                case System.Net.HttpStatusCode.Forbidden:
                    throw new UnauthorizedAccessException(containerName);
                default:
                    throw new Exception(response.StatusCode.ToString());
            }
        }

        /// <summary>
        /// Get a list of all the required containers.
        /// </summary>
        /// <returns></returns>
        public List<ContainerDefinition> GetRequiredContainerDefinitions()
        {
            var list = new List<ContainerDefinition>();

            list.Add(new ContainerDefinition() { ContainerName = "Identity", PartitionKey = "/Id" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_DeviceFlowCodes", PartitionKey = "/SessionId" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_Logins", PartitionKey = "/ProviderKey" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_Passkeys", PartitionKey = "/UserId" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_PersistedGrant", PartitionKey = "/Key" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_Tokens", PartitionKey = "/UserId" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_UserRoles", PartitionKey = "/UserId" });
            list.Add(new ContainerDefinition() { ContainerName = "Identity_Roles", PartitionKey = "/Id" });

            return list;
        }

        /// <summary>
        /// Disposes of the class resources
        /// </summary>
        public void Dispose()
        {
            _client.Dispose();
        }
    }

    /// <summary>
    /// Container definition
    /// </summary>
    public class ContainerDefinition
    {
        public required string ContainerName { get; set; }

        public required string PartitionKey { get; set; }
    }
}
