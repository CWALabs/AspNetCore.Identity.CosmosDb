# AspNetCore.Identity.CosmosDbCompat.Tests

API compatibility test suite for the [`AspNetCore.Identity.CosmosDb`](../AspNetCore.Identity.CosmosDb) library. Its purpose is to confirm that the `UserManager<TUser>` and `RoleManager<TRole>` interoperability contract — the core API surface that existing consumers depend on — **continues to work unchanged as the library is upgraded across .NET versions**.

The test classes in this project originated from the .NET 7 era of the library (note the `AspNetCore.Identity.CosmosDb.Tests.Net7` namespace) and have been preserved as a regression guard. Each time the library is updated to a new .NET target, these tests must still pass to ensure no breaking changes have been introduced to the manager-level API.

> **Note:** This project tests API-level backward compatibility (i.e., "existing code still compiles and behaves the same"). It does **not** test the `backwardCompatibility: true` constructor flag on `CosmosIdentityDbContext`, which is a separate feature for reading older Cosmos DB databases that use `"Discriminator"` instead of `"$type"` as the embedded type discriminator field. That EF Core Cosmos behavior is covered in [`CosmosIdentityDbContextTests`](../AspNetCore.Identity.CosmosDb.Tests/CosmosIdentityDbContextTests.cs).

This project re-uses the shared test infrastructure from [`AspNetCore.Identity.CosmosDb.Tests`](../AspNetCore.Identity.CosmosDb.Tests) and runs its tests against isolated, uniquely named databases to avoid state collisions between the two suites.

- **Framework**: MSTest v4 (Microsoft Testing Platform runner) on .NET 10
- **Coverage**: `Microsoft.Testing.Extensions.CodeCoverage`
- **Reporting**: `Microsoft.Testing.Extensions.TrxReport`
- **Parallelism**: method-level (`[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`)
- **Mocking**: Moq (inherited from the shared test project)

---

## Relationship to the Main Test Suite

| Aspect | `AspNetCore.Identity.CosmosDb.Tests` | `AspNetCore.Identity.CosmosDbCompat.Tests` |
| --- | --- | --- |
| Purpose | Full integration + unit coverage | API compatibility regression across .NET upgrades |
| Database isolation | Shared database per class | Unique database **per class per run** (`{dbName}-compat-{guid}`) |
| Test runner | Classic MSTest runner | Microsoft Testing Platform (`EnableMSTestRunner=true`) |
| Shared infrastructure | Defines `CosmosIdentityTestsBase` / `TestUtilities` | References and reuses them as a project dependency |
| Test content | All stores, managers, passkeys, DI, contexts | `UserManager` and `RoleManager` interoperability only |

---

## Test Classes

| Class | What it covers |
| --- | --- |
| `UserManagerInterOperabilityTests` | `UserManager<IdentityUser>` CRUD, claims, logins, lockout, password, phone, two-factor, email, tokens — in backward-compatibility mode |
| `RoleManagerInterOperabilityTests` | `RoleManager<IdentityRole>` CRUD, claim operations — in backward-compatibility mode |

Each class spins up a dedicated Cosmos DB database with a unique name (e.g., `identity-tests-compat-user-<guid>`) to ensure complete isolation and allow parallel runs without interference.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Azure Cosmos DB account **or** the [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/local-emulator) running locally
- The [`AspNetCore.Identity.CosmosDb.Tests`](../AspNetCore.Identity.CosmosDb.Tests) project (referenced as a build dependency to share `CosmosIdentityTestsBase` and `TestUtilities`)

---

## Configuration

Reads the same two configuration keys as the main test suite:

| Key | Description |
| --- | --- |
| `ApplicationDbContextConnection` | Cosmos DB connection string |
| `CosmosIdentityDbName` | Base name for databases created during the run |

Configuration is resolved in priority order (inherited from `TestUtilities`):

1. **.NET User Secrets** (secrets ID shared with main suite: `e5f1f76e-e6ac-4f45-ac55-02e401325c2b`)
2. **Environment variables** (both original casing and `UPPER_CASE` are tried)
3. **`appsettings.json`** in the test project output directory

### Local setup with User Secrets

```bash
# These are the same secrets used by AspNetCore.Identity.CosmosDb.Tests
dotnet user-secrets --id e5f1f76e-e6ac-4f45-ac55-02e401325c2b \
    set "ApplicationDbContextConnection" "<your-cosmos-connection-string>"

dotnet user-secrets --id e5f1f76e-e6ac-4f45-ac55-02e401325c2b \
    set "CosmosIdentityDbName" "identity-tests"
```

> Because this project shares the same `UserSecretsId` as the main test project, secrets configured for one are automatically available to the other.

### Using the Cosmos DB Emulator

Use the well-known emulator connection string:

```text
AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==
```

See [`AspNetCore.Identity.CosmosDb.Demo/COSMOS_EMULATOR_SETUP.md`](../AspNetCore.Identity.CosmosDb.Demo/COSMOS_EMULATOR_SETUP.md) for a full emulator walkthrough.

### CI / GitHub Actions

Set `APPLICATIONDBCONTEXTCONNECTION` and `COSMOSIDENTITYDBNAME` as repository secrets. The uppercase fallback in `TestUtilities.GetKeyValue()` picks these up automatically.

---

## Running the Tests

### All tests

```bash
cd AspNetCore.Identity.CosmosDbCompat.Tests
dotnet test
```

Because this project uses the Microsoft Testing Platform runner (`EnableMSTestRunner=true`, `OutputType=Exe`), you can also run the test binary directly:

```bash
dotnet run --project AspNetCore.Identity.CosmosDbCompat.Tests
```

### With code coverage

```bash
dotnet test --collect:"Code Coverage"
```

The `Microsoft.Testing.Extensions.CodeCoverage` package writes a `.coverage` file to `TestResults/`. Open it in Visual Studio or convert it to Cobertura format with:

```bash
dotnet coverage convert TestResults/**/*.coverage --output coverage.xml --output-format cobertura
```

### With TRX report output

```bash
dotnet test --report-trx --report-trx-filename compat-results.trx
```

---

## Test Project Structure

```text
AspNetCore.Identity.CosmosDbCompat.Tests/
├── MSTestSettings.cs                   # Assembly-level parallel execution configuration
├── UserManagerInterOperabilityTests.cs # UserManager compat regression tests
└── RoleManagerInterOperabilityTests.cs # RoleManager compat regression tests
```

Shared infrastructure (`CosmosIdentityTestsBase`, `TestUtilities`, and mock helpers) lives in the main test project and is consumed here via a project reference.

---

## Why a Separate Project?

Several concrete reasons drive the separation:

**1. Originally written for a different .NET target**
These tests were authored when the library targeted .NET 7. The class namespace `AspNetCore.Identity.CosmosDb.Tests.Net7` reflects that origin. Keeping them in a separate project preserves their history and makes it clear they serve a regression purpose rather than covering new functionality.

**2. Complete database isolation**
Every test class creates its own uniquely named Cosmos DB database at class initialisation (e.g., `identity-tests-compat-user-<guid>` and `identity-tests-compat-role-<guid>`). This prevents any write made by a compat test from appearing in — or being corrupted by — the shared databases used by the main suite, and allows both suites to run simultaneously on the same Cosmos DB account without coordination.

**3. Different MSTest runner**
The main test project uses the classic MSTest runner. This project uses the [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) (`EnableMSTestRunner=true`, `OutputType=Exe`), which enables direct binary execution (`dotnet run`) and native integration with `dotnet test` without a separate test adapter. Keeping them separate avoids mixing runner configurations in one project file.

**4. Focused surface area**
The compat project intentionally tests only what external consumers call — `UserManager<TUser>` and `RoleManager<TRole>`. Internal store details (`CosmosUserStore`, `CosmosRoleStore`) and new features (passkeys, container utilities) are tested in the main suite. This narrow scope makes failures in the compat project immediately meaningful: if a compat test breaks, a public API has regressed.

---

## Related Projects

| Project | Description |
| --- | --- |
| [`AspNetCore.Identity.CosmosDb`](../AspNetCore.Identity.CosmosDb) | The library under test |
| [`AspNetCore.Identity.CosmosDb.Tests`](../AspNetCore.Identity.CosmosDb.Tests) | Primary test suite (also provides shared test infrastructure) |
| [`AspNetCore.Identity.CosmosDb.Demo`](../AspNetCore.Identity.CosmosDb.Demo) | Demo web application |
