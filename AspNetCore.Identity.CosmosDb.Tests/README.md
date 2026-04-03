# AspNetCore.Identity.CosmosDb.Tests

Primary test suite for the [`AspNetCore.Identity.CosmosDb`](../AspNetCore.Identity.CosmosDb) library. Contains both unit tests (no external dependencies) and integration tests that run against a live Azure Cosmos DB instance or the [Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator).

- **Framework**: MSTest v4 on .NET 10
- **Coverage**: `coverlet.collector`
- **Mocking**: Moq

---

## Test Categories

### Unit Tests (no Cosmos DB required)

These tests run without any external infrastructure and can be executed in any environment.

| Class | What it covers |
| --- | --- |
| `ServiceCollectionExtensionsTests` | `AddCosmosIdentity<>()` DI registration — verifies that `IUserStore`, `IRoleStore`, and `IRepository` are registered correctly |
| `IdentityStoreBaseTests` | `IdentityStoreBase` dispose guard (`ThrowIfDisposed`) and `ProcessExceptions` error mapping |
| `PersonalDataConverterTests` | `PersonalDataConverter` encryption/decryption round-trip |
| `RetryTests` | `Retry.Do()` helper — retry logic, delay intervals, exception propagation |
| `TestUtilitiesTests` | Sanity checks for shared test helpers |

### Integration Tests (Azure Cosmos DB required)

These tests create and manipulate real data in a Cosmos DB database. Each test class initialises its own stores and the suite shares a single database configured via the settings described below.

| Class | What it covers |
| --- | --- |
| `CosmosIdentityDbContextTests` | `CosmosIdentityDbContext<TUser,TRole,TKey>` model-building with and without backward-compatibility mode |
| `Stores/CosmosUserStoreTests` | Full CRUD lifecycle for `CosmosUserStore` — create, find, update, delete |
| `Stores/CosmosUserStoreClaimsTests` | `IUserClaimStore` — add, replace, remove, and query claims |
| `Stores/CosmosUserStorePasskeyTests` | `IUserPasskeyStore` — add/update, retrieve, and remove passkey credentials |
| `Stores/CosmosRoleStoreTests` | Full CRUD lifecycle for `CosmosRoleStore` |
| `Containers/ContainerUtilitiesTests` | `ContainerUtilities` — database and container provisioning |
| `UserManagerInterOperabilityTests` | `UserManager<IdentityUser>` end-to-end with the Cosmos stores — covers the full `IUserStore` surface (name, ID, email, phone, password, lockout, two-factor, tokens, logins, …) |
| `RoleManagerInterOperabilityTests` | `RoleManager<IdentityRole>` end-to-end — create, find, claims, normalisation |
| `UserManagerPasskeyInterOperabilityTests` | `UserManager<IdentityUser>` passkey operations — set, find by credential ID, and remove |
| `SignInManagerPasskeyInterOperabilityTests` | Passkey credential lookup scenarios used during sign-in: valid credential ID, invalid credential ID, multiple passkeys per user |
| `PasskeyStressAndReliabilityTests` | Concurrent passkey additions, large passkey collections (50+ per user), edge cases (empty byte arrays, very long names), duplicate credential IDs |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Azure Cosmos DB account **or** the [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator) running locally

---

## Configuration

Integration tests read two required configuration keys:

| Key | Description |
| --- | --- |
| `ApplicationDbContextConnection` | Cosmos DB connection string |
| `CosmosIdentityDbName` | Name of the database to use for tests |

Configuration is resolved in priority order:

1. **.NET User Secrets** (recommended for local development — secrets ID: `e5f1f76e-e6ac-4f45-ac55-02e401325c2b`)
2. **Environment variables** (used by CI — both original casing and `UPPER_CASE` are tried)
3. **`appsettings.json`** in the test project output directory

### Local setup with User Secrets

```bash
# Set the connection string
dotnet user-secrets --id e5f1f76e-e6ac-4f45-ac55-02e401325c2b \
    set "ApplicationDbContextConnection" "<your-cosmos-connection-string>"

# Set the database name
dotnet user-secrets --id e5f1f76e-e6ac-4f45-ac55-02e401325c2b \
    set "CosmosIdentityDbName" "identity-tests"
```

### Using the Cosmos DB Emulator

1. Start the emulator (defaults to `https://localhost:8081/`).
2. Use the well-known emulator connection string:

```text
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

See [`AspNetCore.Identity.CosmosDb.Demo/COSMOS_EMULATOR_SETUP.md`](../AspNetCore.Identity.CosmosDb.Demo/COSMOS_EMULATOR_SETUP.md) for a full emulator walkthrough.

### CI / GitHub Actions

Set the two keys as repository secrets (`APPLICATIONDBCONTEXTCONNECTION` and `COSMOSIDENTITYDBNAME`). `TestUtilities` automatically tries the uppercase form of each key when the lowercase form is not found.

---

## Running the Tests

### All tests (unit + integration)

```bash
cd AspNetCore.Identity.CosmosDb.Tests
dotnet test
```

### Unit tests only (no Cosmos DB needed)

Use the VS Code Test Explorer filter or the `--filter` flag:

```bash
dotnet test --filter "FullyQualifiedName~ServiceCollectionExtensions|FullyQualifiedName~IdentityStoreBase|FullyQualifiedName~PersonalDataConverter|FullyQualifiedName~Retry|FullyQualifiedName~TestUtilities"
```

### With code coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

Results are written to a `TestResults/` subdirectory. Use [ReportGenerator](https://github.com/danielpalme/ReportGenerator) to produce an HTML report:

```bash
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport" -reporttypes:Html
```

---

## Test Project Structure

```text
AspNetCore.Identity.CosmosDb.Tests/
├── CosmosIdentityTestsBase.cs          # Abstract base class — shared store setup, mock helpers
├── TestUtilities.cs                    # Configuration loading, store factories, test constants
├── ServiceCollectionExtensionsTests.cs
├── IdentityStoreBaseTests.cs
├── CosmosIdentityDbContextTests.cs
├── PersonalDataConverterTests.cs
├── RetryTests.cs
├── UserManagerInterOperabilityTests.cs
├── RoleManagerInterOperabilityTests.cs
├── UserManagerPasskeyInterOperabilityTests.cs
├── SignInManagerPasskeyInterOperabilityTests.cs
├── PasskeyStressAndReliabilityTests.cs
├── TestUtilitiesTests.cs
├── Stores/
│   ├── CosmosUserStoreTests.cs
│   ├── CosmosUserStoreClaimsTests.cs
│   ├── CosmosUserStorePasskeyTests.cs
│   └── CosmosRoleStoreTests.cs
├── Containers/
│   └── ContainerUtilitiesTests.cs
└── EntityConfigurations/
    └── UserEntityTypeConfigurationTests.cs
```

---

## Shared Test Infrastructure

### `CosmosIdentityTestsBase`

Abstract base class inherited by all integration test classes. Provides:

- `InitializeClass(connectionString, databaseName)` — wires up `TestUtilities`, which provisions the stores
- `GetMockRandomUserAsync(userStore, saveToDatabase)` — creates a random `IdentityUser` (optionally persisted)
- `GetMockRandomRoleAsync(roleStore, saveToDatabase)` — creates a random `IdentityRole` (optionally persisted)
- `GetTestUserManager(userStore)` / `GetTestRoleManager(roleStore)` — returns fully configured manager instances
- `CreatePasskeyInfo(name)` — builds a `UserPasskeyInfo` stub for passkey tests

### `TestUtilities`

- `GetConfig()` — loads `appsettings.json`, environment variables, and user secrets
- `GetKeyValue(key)` — resolves a config key with fallback to uppercase env vars
- `GetUserStore(connectionString, databaseName)` — returns a `CosmosUserStore<IdentityUser, IdentityRole, string>`
- `GetDbContext(connectionString, databaseName)` — returns a `CosmosIdentityDbContext` instance
- `GetContainerUtilities(connectionString, databaseName)` — returns a `ContainerUtilities` instance

---

## Related Projects

| Project | Description |
| --- | --- |
| [`AspNetCore.Identity.CosmosDb`](../AspNetCore.Identity.CosmosDb) | The library under test |
| [`AspNetCore.Identity.CosmosDbCompat.Tests`](../AspNetCore.Identity.CosmosDbCompat.Tests) | Backward-compatibility test suite |
| [`AspNetCore.Identity.CosmosDb.Demo`](../AspNetCore.Identity.CosmosDb.Demo) | Demo web application |
