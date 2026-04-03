# Setting up Cosmos DB Emulator

This guide walks you through setting up the Azure Cosmos DB Emulator for local development with the AspNetCore.Identity.CosmosDb demo application.

## Prerequisites

### System Requirements
- **Windows**: Windows 10/11 (Pro, Enterprise, or Education)
- **macOS**: macOS 10.12 (Sierra) or later (using Docker)
- **Linux**: Ubuntu 20.04+ or equivalent (using Docker)

### Hardware Requirements
- At least 2 GB RAM (4 GB recommended)
- At least 10 GB free disk space

### Software Requirements
- **.NET 10 SDK** (already installed if you're running the demo)
- **Docker** (optional, for non-Windows systems or if you prefer containerized setup)

## Installation

### Option 1: Native Windows Installation (Recommended for Windows)

1. **Download the Emulator**
   - Visit: https://aka.ms/cosmosdb-emulator
   - Download the latest version for Windows

2. **Install**
   ```powershell
   # Run the installer
   Start-Process -FilePath "CosmosDBEmulator.exe"
   ```
   
   Follow the installation wizard and accept the default settings.

3. **Verify Installation**
   
   The emulator should be in one of these locations:
   ```
   C:\Program Files\Azure Cosmos DB Emulator\
   C:\Program Files (x86)\Azure Cosmos DB Emulator\
   ```

### Option 2: Docker Installation (For any OS)

1. **Pull the Docker Image**
   ```bash
   docker pull mcr.microsoft.com/cosmosdb/windows/azure-cosmos-emulator:latest
   ```
   
   Or for Linux:
   ```bash
   docker pull mcr.microsoft.com/cosmosdb/linux:latest
   ```

2. **Run the Container**
   
   **Windows Docker:**
   ```powershell
   docker run -p 8081:8081 `
     -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=3 `
     -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true `
     mcr.microsoft.com/cosmosdb/windows/azure-cosmos-emulator:latest
   ```

   **Linux Docker:**
   ```bash
   docker run -p 8081:8081 \
     -e AZURE_COSMOS_EMULATOR_PARTITION_COUNT=3 \
     -e AZURE_COSMOS_EMULATOR_ENABLE_DATA_PERSISTENCE=true \
     -e AZURE_COSMOS_EMULATOR_KEY="C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLMZD+2d9M2wgD3vK8MyZN+DynERMCa3aSikZLecbGiujTjZtLWVLQECoj4X3R+O7apIDTPqyv6mQ==" \
     mcr.microsoft.com/cosmosdb/linux:latest
   ```

3. **Wait for Startup**
   
   The emulator may take 1-3 minutes to start. You'll see logs indicating when it's ready.

## Starting the Emulator

### Windows Native
1. Press Windows key and search for "Cosmos DB Emulator"
2. Click the application to start it
3. It will start automatically on `https://localhost:8081`
4. Open the emulator UI (aka `Data Explorer`) at `https://localhost:8081/_explorer/index.html`
5. Find the primary connection string in the emulator UI under "Connection String" section.
6. Copy the connection string and paste it into the `appsettings.json` of this project (`ConnectionStrings:CosmosDb`).

Now, you can run the demo application and it will connect to the emulator.

**Or via command line:**
```powershell
C:\"Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe"
```

### Via Docker
The container starts automatically when you run the docker command (see Option 2 above).

Keep the terminal window open - closing it will stop the container.

## Verify the Emulator is Running

### Method 1: Check the Certificate
```powershell
# The emulator installs a self-signed certificate
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.Subject -match "localhost" }
```

### Method 2: Test Connection via Browser
- Navigate to: `https://localhost:8081/_explorer/index.html`
- You should see the Cosmos DB Explorer UI

### Method 3: Test via PowerShell
```powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri "https://localhost:8081/" -Certificate (Get-Item Cert:\CurrentUser\Root\*) | Select-Object StatusCode
```

### Method 4: Test via .NET
```csharp
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;

var client = new HttpClient(handler);
var response = await client.GetAsync("https://localhost:8081/");
Console.WriteLine($"Status: {response.StatusCode}");
```

## Configuration

### Connection String
The emulator uses this default connection string:
```
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLMZD+2d9M2wgD3vK8MyZN+DynERMCa3aSikZLecbGiujTjZtLWVLQECoj4X3R+O7apIDTPqyv6mQ==
```

This is already configured in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLMZD+2d9M2wgD3vK8MyZN+DynERMCa3aSikZLecbGiujTjZtLWVLQECoj4X3R+O7apIDTPqyv6mQ=="
  }
}
```

### Database Name
The default database name is `AspNetCoreIdentity` (configured in `appsettings.json`).

To change it, update:
```json
{
  "CosmosDb": {
    "DatabaseName": "YourDatabaseName"
  }
}
```

## Disabling Certificate Validation (Development Only)

If you encounter SSL/certificate errors, you can disable validation in development:

**In Program.cs:**
```csharp
#if DEBUG
// Development only - disable certificate validation for Cosmos DB Emulator
System.Net.ServicePointManager.ServerCertificateValidationCallback = (_, _, _, _) => true;
#endif
```

Or in your DbContext setup:
```csharp
var handler = new HttpClientHandler();
#if DEBUG
handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true;
#endif
```

## Running the Demo Application

Once the emulator is running, start the demo:

```bash
cd AspNetCore.Identity.CosmosDb.Demo
dotnet run
```

The application will:
1. Connect to the emulator at `https://localhost:8081`
2. Create the database `AspNetCoreIdentity` (if it doesn't exist)
3. Create the necessary containers (AspNetUsers, AspNetRoles, AspNetUserPasskeys, etc.)
4. Start on `https://localhost:5001` (or similar)

## Troubleshooting

### "Unable to connect to localhost:8081"

1. **Verify emulator is running**
   ```powershell
   # Windows - check if the process is running
   Get-Process | Where-Object { $_.ProcessName -like "*Cosmos*" }
   ```

2. **Check firewall**
   - Ensure port 8081 is not blocked
   - Check Windows Defender Firewall settings

3. **Reset the emulator** (Windows)
   ```powershell
   # This clears all data
   C:\"Program Files\Azure Cosmos DB Emulator\CosmosDB.Emulator.exe" /Reset
   ```

### "Certificate not trusted" errors

1. **Install the certificate** (Windows)
   ```powershell
   # The emulator automatically installs its certificate
   # If it doesn't, manually import it:
   $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2("C:\Program Files\Azure Cosmos DB Emulator\CosmosDBEmulator.pfx")
   ```

2. **Disable certificate validation** (Development only)
   - See "Disabling Certificate Validation" section above

### High CPU usage

- The emulator uses significant CPU during startup and indexing
- It's normal for 30 seconds to a few minutes
- If persistent, reduce partition count: `/PartitionCount=1`

### Data persistence issues

- Data is stored in `%LOCALAPPDATA%\CosmosDBEmulator\`
- To reset: Delete this folder or use `/Reset` flag

### Memory issues

- The emulator may consume 1-3 GB of RAM
- If running Docker, increase Docker's memory allocation

## Using Azure Cosmos DB Instead

For production-like testing, use an Azure Cosmos DB free tier account:

1. Create a free Azure account at https://azure.microsoft.com/free
2. Create a Cosmos DB resource
3. Get the connection string from Azure Portal
4. Update `appsettings.json`

## Performance Considerations

### Emulator Limitations
- Single partition (default)
- No geo-replication
- Limited throughput (RU/s)
- No autoscale

### Recommended Settings for Development
```powershell
# Start with higher partition count for testing
CosmosDB.Emulator.exe /PartitionCount=4
```

### Connection Pooling
The .NET SDK automatically manages connection pooling. For best results:

```csharp
// In Program.cs, configure CosmosClient pooling
options.UseCosmos(
    accountEndpoint: cosmosConnectionString,
    databaseName: cosmosDatabaseName,
    cosmosClientOptions: new CosmosClientOptions
    {
        MaxRetryAttemptsOnRateLimitedRequests = 9,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
    }
);
```

## Additional Resources

- **Official Docs**: https://docs.microsoft.com/azure/cosmos-db/local-emulator
- **Docker Hub**: https://hub.docker.com/_/microsoft-azure-cosmos-emulator
- **Emulator Import/Export**: https://docs.microsoft.com/azure/cosmos-db/local-emulator-export-ssl-certificates
- **Firewall Configuration**: https://docs.microsoft.com/azure/cosmos-db/local-emulator#running-on-docker

## Quick Start Checklist

- [ ] Download/Install Cosmos DB Emulator or start Docker container
- [ ] Verify emulator is running (https://localhost:8081)
- [ ] Verify certificate is installed/trusted
- [ ] Check `appsettings.json` has correct connection string
- [ ] Run demo application: `dotnet run`
- [ ] Navigate to `https://localhost:5001` (or shown port)
- [ ] Test PassKey functionality
- [ ] View data in Cosmos DB Explorer (https://localhost:8081/_explorer)

## Next Steps

Once the emulator is set up:
1. Run the demo application
2. Register a user account
3. Test PassKey registration
4. View data in Azure Cosmos DB Explorer
5. Run smoke tests: `dotnet test AspNetCore.Identity.CosmosDb.Demo.Tests`
