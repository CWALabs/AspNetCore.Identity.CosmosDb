# Contributing

Thank you for contributing to this project.

## Before You Start

- Review the current documentation in [README.md](README.md).
- Read the repository [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).
- Open an issue before starting large or breaking changes.

## Development Setup

This repository targets .NET 10 and uses Azure Cosmos DB for its integration-style tests.

### Prerequisites

- .NET 10 SDK
- An Azure Cosmos DB account or local equivalent for test execution
- User secrets or equivalent configuration for test settings

### Required Test Settings

The current tests expect these settings:

```json
{
  "ApplicationDbContextConnection": "AccountEndpoint=...;AccountKey=...;",
  "CosmosIdentityDbName": "localtests"
}
```

The test projects create unique database names during execution, so the configured database name is used as a prefix rather than a fixed shared database.

## Build And Test

Build the solution:

```powershell
dotnet build AspNetCore.Identity.CosmosDb.sln
```

Run the main test suite:

```powershell
dotnet test AspNetCore.Identity.CosmosDb.Tests\AspNetCore.Identity.CosmosDb.Tests.csproj
```

Run the compatibility suite:

```powershell
dotnet test AspNetCore.Identity.CosmosDbCompat.Tests\AspNetCore.Identity.CosmosDbCompat.Tests.csproj
```

Run all tests:

```powershell
dotnet test AspNetCore.Identity.CosmosDb.sln
```

## Contribution Expectations

- Keep changes focused on the issue being addressed.
- Preserve public API compatibility unless the change is explicitly intended to be breaking.
- Prefer asynchronous Cosmos-backed code paths for new implementation work.
- Add or update tests with production changes.
- Update documentation when behavior, setup, or supported scenarios change.

## Pull Requests

Please include:

- A concise description of the change
- Why the change is needed
- Test coverage added or updated
- Any compatibility considerations for existing consumers

If your change affects Identity behavior, Cosmos model conventions, passkeys, or startup configuration, include a small repro or usage example in the pull request description.

## Reporting Issues

- Use the bug report template for defects.
- Use the feature request template for enhancements.
- For security issues, follow the process in [SECURITY.md](SECURITY.md).
