# Cosmos DB Provider for ASP.NET Core Identity

[![CodeQL](https://github.com/MoonriseSoftwareCalifornia/AspNetCore.Identity.CosmosDb/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/MoonriseSoftwareCalifornia/AspNetCore.Identity.CosmosDb/actions/workflows/codeql-analysis.yml)
![Net 9 Tests (192)](https://github.com/MoonriseSoftwareCalifornia/AspNetCore.Identity.CosmosDb/actions/workflows/donet9tests.yml/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Identity.CosmosDb.svg)](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)

`AspNetCore.Identity.CosmosDb` provides ASP.NET Core Identity stores backed by the Entity Framework Core Azure Cosmos DB provider.

The current package line targets `.NET 10` and Entity Framework Core 10.

## Projects That Use This Library

- [Cosmos CMS](https://cosmos.moonrise.net/) ([GitHub](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS))

If you would like your project listed here, open an issue or pull request.

## Package Highlights

- Uses Entity Framework Core Cosmos for user, role, claim, token, login, and passkey persistence
- Supports generic keys
- Supports LINQ queries through the standard Identity `Users` and `Roles` query surfaces
- Preserves compatibility behavior needed for older EF Core Cosmos-backed databases

## Cosmos Container Layout

This package uses eight Cosmos containers.

If throughput is provisioned at the container level, this can increase the minimum RU requirement for the account. To reduce cost, prefer shared database-level throughput and evaluate autoscale when appropriate.

- [Database throughput guidance](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-provision-database-throughput?tabs=dotnetv2#provision-throughput-using-azure-portal)
- [Autoscale throughput guidance](https://learn.microsoft.com/en-us/azure/cosmos-db/provision-throughput-autoscale)

The required containers include support for user passkeys in addition to the standard Identity entities.

## Installation

Install the NuGet package:

```powershell
Install-Package AspNetCore.Identity.CosmosDb
```

Create an Azure Cosmos DB account and choose a throughput model that matches your workload. For development and test scenarios, free tier or serverless is usually the simplest starting point.

## Application Configuration

An example `secrets.json` file:

```json
{
  "SetupCosmosDb": "true",
  "CosmosIdentityDbName": "YourDatabaseName",
  "ConnectionStrings": {
  "ApplicationDbContextConnection": "AccountEndpoint=...;AccountKey=...;"
  }
}
```

`SetupCosmosDb` is intended for initial provisioning. Remove or disable it after the database and containers have been created to avoid unnecessary startup work.

## DbContext Setup

Inherit from `CosmosIdentityDbContext<TUser, TRole, TKey>`:

```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AspNetCore.Identity.CosmosDb.Example.Data
{
  public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>
  {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
    {
    }
  }
}
```

## Program.cs Setup

Typical registration looks like this:

```csharp
using AspNetCore.Identity.CosmosDb;
using AspNetCore.Identity.CosmosDb.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContextConnection");
var cosmosIdentityDbName = builder.Configuration.GetValue<string>("CosmosIdentityDbName");
var setupCosmosDb = builder.Configuration.GetValue<bool>("SetupCosmosDb");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
  options.UseCosmos(connectionString!, cosmosIdentityDbName!));

builder.Services
  .AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(options =>
  {
    options.SignIn.RequireConfirmedAccount = true;
  })
  .AddDefaultUI()
  .AddDefaultTokenProviders();
```

If you want the application to provision the Cosmos database and required containers during initial startup:

```csharp
if (setupCosmosDb)
{
  var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
  optionsBuilder.UseCosmos(connectionString!, cosmosIdentityDbName!);

  using var dbContext = new ApplicationDbContext(optionsBuilder.Options);
  dbContext.Database.EnsureCreated();
}
```

## Repository Guidance

The internal repository abstraction still exposes a small set of synchronous members for compatibility with older callers, but Cosmos-backed execution should prefer asynchronous APIs. New code should use the async repository members.

## Passkey Support

Passkey persistence is part of the required Cosmos setup for current versions of this package. If your application uses ASP.NET Core Identity passkeys, ensure the database and containers have been provisioned with the current model before enabling the feature in production.

## Backward Compatibility For Older EF Core Cosmos Databases

Entity Framework Core 9 changed important Cosmos behaviors that affect existing Identity databases.

### Discriminator In JSON IDs

This package applies `HasDiscriminatorInJsonIds()` so that generated Cosmos IDs continue to include the entity discriminator.

### Embedded Discriminator Name

Older databases may use `Discriminator` instead of `$type` for the embedded discriminator name. To read those databases, construct your context with `backwardCompatibility: true`.

Example:

```csharp
var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseCosmos(connectionString, databaseName);

using var dbContext = new ApplicationDbContext(optionsBuilder.Options, backwardCompatibility: true);
```

### Index Definitions

EF Core Cosmos no longer ignores index configuration in the same way as earlier releases. If your model still defines index configuration inherited from relational assumptions, remove that configuration rather than relying on it being ignored.

## Upgrading From Version 2.x To 8.x+

If you are upgrading older applications, the `CosmosIdentityDbContext` and `AddCosmosIdentity` registrations now require the key type.

Old form:

```csharp
public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole>
```

Current form:

```csharp
public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>
```

Old form:

```csharp
builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole>()
```

Current form:

```csharp
builder.Services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>()
```

## External Authentication Providers

This library works with external authentication providers. Example packages:

- [Microsoft.AspNetCore.Authentication.Google](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.Google)
- [Microsoft.AspNetCore.Authentication.MicrosoftAccount](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.MicrosoftAccount)

Example registration:

```csharp
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
{
  builder.Services.AddAuthentication().AddGoogle(options =>
  {
    options.ClientId = googleClientId;
    options.ClientSecret = googleClientSecret;
  });
}

var microsoftClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
var microsoftClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];

if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
{
  builder.Services.AddAuthentication().AddMicrosoftAccount(options =>
  {
    options.ClientId = microsoftClientId;
    options.ClientSecret = microsoftClientSecret;
  });
}
```

See the current ASP.NET Core authentication documentation for the latest guidance on external login providers.

## Querying Users And Roles

Both the user and role stores support LINQ queries through Entity Framework Core.

```csharp
var userResults = userManager.Users.Where(u => u.Email!.StartsWith("bob"));
var roleResults = roleManager.Roles.Where(r => r.Name!.Contains("water"));
```

For provider-specific LINQ limitations, see the Azure Cosmos DB LINQ documentation:

- [Supported LINQ operations](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/linq-to-sql)

## Tests

The automated tests use Cosmos-backed execution paths.

Current local test configuration:

```json
{
  "CosmosDB": "AccountEndpoint=...;AccountKey=...;",
  "CosmosIdentityDbName": "localtests"
}
```

The test suites create unique database names during execution to avoid destructive interference between runs.

## Example Application

For a larger application using this package, see Cosmos CMS:

- [Program.cs example](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/blob/main/Editor/Program.cs)
- [Cosmos CMS repository](https://github.com/MoonriseSoftwareCalifornia/CosmosCMS/tree/main)

## Bugs And Support

If you find a bug, open a GitHub issue with a minimal repro when possible.

- [GitHub issues](https://github.com/MoonriseSoftwareCalifornia/AspNetCore.Identity.CosmosDb/issues)
- [NuGet package](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)

## Changelog

This changelog lists notable changes beyond routine package dependency updates.

### 10.0.5.1

- Updated the package to .NET 10 and Entity Framework Core 10.
- Added explicit passkey container support to setup guidance and infrastructure.
- Added async-first repository guidance while retaining compatibility wrappers for synchronous callers.

### 9.0.1.0

- Removed the old bundled sample application.
- Pointed users to Cosmos CMS as a maintained example application.

### 9.0.0.3

- Added backward compatibility support for databases created with EF Core 8 or earlier.
- Added tests covering backward compatibility behavior.

### 9.0.0.1

- Updated the package for .NET 9 and Entity Framework Core 9.

### 2.0.5.1

- Added IQueryable support for user and role stores.

### 2.0.1.0

- Added an example web project.

### 2.0.0-alpha

- Forked from [pierodetomi/efcore-identity-cosmos](https://github.com/pierodetomi/efcore-identity-cosmos).
- Refactored the package for .NET 6.
- Added `UserStore`, `RoleStore`, `UserManager`, and `RoleManager` tests.
- Renamed the package namespace to `AspNetCore.Identity.CosmosDb`.
- Implemented `IUserLockoutStore<TUser>`.

### 1.0.5

- Added `IUserPhoneNumberStore<TUser>` support.

### 1.0.4

- Added `IUserEmailStore<TUser>` support.
