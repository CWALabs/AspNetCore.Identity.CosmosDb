# AspNetCore.Identity.CosmosDb

[![CodeQL](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/codeql-analysis.yml)
[![Build and Test](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml/badge.svg)](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml)
[![Coverage](https://codecov.io/gh/CWALabs/AspNetCore.Identity.CosmosDb/branch/main/graph/badge.svg)](https://codecov.io/gh/CWALabs/AspNetCore.Identity.CosmosDb)
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Identity.CosmosDb.svg)](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)

`AspNetCore.Identity.CosmosDb` is a .NET 10 ASP.NET Core Identity provider backed by Azure Cosmos DB and EF Core Cosmos. It stores users, roles, claims, tokens, external logins, and passkeys in Cosmos DB while preserving the standard ASP.NET Core Identity programming model.

## Versioning Note

Package versions now follow SemVer 2.0 for NuGet publishing and no longer mirror the target .NET version number.

This means you may see a larger version jump (for example, from a .NET-aligned version to `12.0.0`) without an equivalent framework jump. The target framework and compatibility are still documented separately in this README and in release notes.

## Why use this package

- Cosmos DB-backed ASP.NET Core Identity without a relational database
- Standard `UserManager<TUser>` and `RoleManager<TRole>` support
- Passkey support for .NET 10 Identity
- Generic key support
- Reusable passkey endpoints and packaged JavaScript client

## Screenshots

### Home

![Home page screenshot](https://raw.githubusercontent.com/CWALabs/AspNetCore.Identity.CosmosDb/main/AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Index_cshtml.png)

### Login

![Login page screenshot](https://raw.githubusercontent.com/CWALabs/AspNetCore.Identity.CosmosDb/main/AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Login_cshtml.png)

### Passkeys

![Passkeys page screenshot](https://raw.githubusercontent.com/CWALabs/AspNetCore.Identity.CosmosDb/main/AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Passkeys_cshtml.png)

## Install

```powershell
dotnet add package AspNetCore.Identity.CosmosDb
```

## Basic setup

```csharp
using AspNetCore.Identity.CosmosDb;
using AspNetCore.Identity.CosmosDb.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var connectionString = builder.Configuration.GetConnectionString("CosmosDb")!;
var databaseName = builder.Configuration["CosmosDb:DatabaseName"]!;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseCosmos(connectionString, databaseName));

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
    options.ServerDomain = builder.Configuration["Passkeys:ServerDomain"]!;
});

builder.Services
    .AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();
```

Create your context by inheriting from `CosmosIdentityDbContext<TUser, TRole, TKey>` and call `EnsureCreatedAsync()` during startup to provision the Cosmos database and required containers.

## Passkeys Integration

The package includes passkey persistence plus reusable passkey API endpoints and a packaged JavaScript client.

```csharp
builder.Services.AddCosmosPasskeyUiIntegration(options =>
{
    options.RoutePrefix = "/identity/passkeys";
    options.ClientScriptPath = "/identity/passkeys/client.js";
});

app.MapCosmosPasskeyUiEndpoints<IdentityUser>();
```

## Repository and examples

- Repository: [CWALabs/AspNetCore.Identity.CosmosDb](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb)
- Demo website example: [AspNetCore.Identity.CosmosDb.Demo](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/tree/main/AspNetCore.Identity.CosmosDb.Demo)
- Full demo app template package: [AspNetCore.Identity.CosmosDb.Demo.Template](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb.Demo.Template)
- Razor page templates package: [AspNetCore.Identity.CosmosDb.Templates](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb.Templates)
- Templates source in this repo: [AspNetCore.Identity.Razor.PassKeyPage](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/tree/main/AspNetCore.Identity.Razor.PassKeyPage)
- SkyCMS project: [CWALabs/SkyCMS](https://github.com/CWALabs/SkyCMS)

This package is part of the SkyCMS project and the demo app in this repository is the best end-to-end reference implementation.

## Compatibility

- .NET 10
- ASP.NET Core Identity
- Azure Cosmos DB or Azure Cosmos DB Emulator

## License

MIT
