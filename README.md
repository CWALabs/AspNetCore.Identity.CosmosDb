# ASP.NET Core Identity Provider for Azure Cosmos DB

[![Build and Test](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml/badge.svg)](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/AspNetCore.Identity.CosmosDb.svg)](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)

`AspNetCore.Identity.CosmosDb` is a Cosmos DB-backed implementation of ASP.NET Core Identity built on Entity Framework Core Cosmos. It gives ASP.NET Core applications a non-relational Identity store with support for users, roles, claims, tokens, external logins, and passkeys.

> [!IMPORTANT]
> **Breaking change:** The repository has moved to `.NET 10` and now includes first-class ASP.NET Core Identity passkey support.
> See [`.NET 10 + Passkey Upgrade Summary`](BREAKING_CHANGES_NET10.md) for migration-impact details and change scope.

[Jump to install instructions](#install-in-an-aspnet-core-mvc-or-razor-pages-app)

This repository is part of the [SkyCMS](https://github.com/CWALabs/SkyCMS) ecosystem and contains the core package, a runnable demo site, passkey page templates, and the test suites used to validate the library.

## Repository Contents

- `AspNetCore.Identity.CosmosDb/`: the main library and NuGet package
- `AspNetCore.Identity.CosmosDb.Demo/`: sample ASP.NET Core app showing the package in use
- `AspNetCore.Identity.CosmosDb.Demo.Template/`: `dotnet new` project template for scaffolding the complete demo app
- `AspNetCore.Identity.Razor.PassKeyPage/`: `dotnet new` templates for passkey login and passkey management pages
- `AspNetCore.Identity.CosmosDb.Tests/`: primary automated test suite
- `AspNetCore.Identity.CosmosDbCompat.Tests/`: API compatibility regression suite used during framework and package upgrades
- `PASSKEY_DEVELOPER_GUIDE.md`: passkey integration guidance

## Packages

- [`AspNetCore.Identity.CosmosDb`](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb): main Identity provider package
- [`AspNetCore.Identity.CosmosDb.Demo.Template`](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb.Demo.Template): full demo app `dotnet new` project template
- [`AspNetCore.Identity.CosmosDb.Templates`](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb.Templates): Razor page templates for passkey UI integration

## Release And Versioning

This repository uses a shared solution version stored in [Directory.Build.props](Directory.Build.props). All projects in the solution inherit that version so package, demo, and test artifacts stay aligned.

For local release preparation, use the PowerShell helpers in [scripts/README.md](scripts/README.md) instead of manually editing version numbers or creating tags by hand.

Both release helpers require a clean git working tree. If there are any uncommitted changes, they halt immediately before updating the version, creating a commit, or tagging a release.

`New-ReleaseTag.ps1` also only runs from `main` by default. If you ever need to release from another branch intentionally, use `-AllowNonMainBranch`.

Recommended release flow:

```powershell
# Interactive release flow: bump version, commit it, create tag, optionally push
.\scripts\New-ReleaseTag.ps1
```

Useful command-line examples:

```powershell
# Preview the next patch release without changing anything
.\scripts\New-ReleaseTag.ps1 -ChangeType Patch -NoPush -WhatIf

# Create and push a stable minor release
.\scripts\New-ReleaseTag.ps1 -ChangeType Minor -Push

# Create and push a release candidate tag
.\scripts\New-ReleaseTag.ps1 -ChangeType Patch -ReleaseCandidateNumber 1 -Push

# Only bump the shared repo version without tagging
.\scripts\Set-RepoVersion.ps1 -ChangeType Patch -Commit
```

The NuGet publish workflow validates that the pushed tag matches `RepoVersion` before packaging and publishing. In practice, that means:

- stable release tags should look like `v12.0.1`
- release candidate tags should look like `v12.0.1-rc1`
- the `12.0.1` part must match `RepoVersion` in [Directory.Build.props](Directory.Build.props)

## What The Library Does

- Persists ASP.NET Core Identity data in Azure Cosmos DB through EF Core Cosmos
- Implements the standard ASP.NET Core Identity store abstractions for `UserManager<TUser>` and `RoleManager<TRole>`
- Supports generic key types
- Adds passkey / WebAuthn persistence through `IUserPasskeyStore<TUser>`
- Provides reusable passkey API endpoints and packaged browser-side JavaScript
- Supports older Cosmos-backed Identity databases through backward-compatibility mode

## Requirements

- .NET 10
- Azure Cosmos DB or the [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/emulator)
- ASP.NET Core Identity

## Install In An ASP.NET Core MVC Or Razor Pages App

The registration is the same for MVC and Razor Pages. The only difference is whether you add `AddControllersWithViews()` or `AddRazorPages()` and how you map routes.

If you want a complete working reference before wiring this up yourself, see the demo app in [AspNetCore.Identity.CosmosDb.Demo/README.md](AspNetCore.Identity.CosmosDb.Demo/README.md).

### 1. Install the package

```powershell
Install-Package AspNetCore.Identity.CosmosDb
```

or:

```powershell
dotnet add package AspNetCore.Identity.CosmosDb
```

### 2. Add configuration

```json
{
  "ConnectionStrings": {
    "CosmosDb": "AccountEndpoint=https://<account>.documents.azure.com:443/;AccountKey=<key>;"
  },
  "CosmosDb": {
    "DatabaseName": "MyIdentityDb"
  },
  "Passkeys": {
    "ServerDomain": "localhost"
  }
}
```

### 3. Create a DbContext

```csharp
using AspNetCore.Identity.CosmosDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
```

### 4. Register services in Program.cs

```csharp
using AspNetCore.Identity.CosmosDb;
using AspNetCore.Identity.CosmosDb.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CosmosDb")!;
var databaseName = builder.Configuration["CosmosDb:DatabaseName"]!;
var passkeyServerDomain = builder.Configuration["Passkeys:ServerDomain"];

if (string.IsNullOrWhiteSpace(passkeyServerDomain))
{
  if (builder.Environment.IsDevelopment())
  {
    passkeyServerDomain = "localhost";
  }
  else
  {
    throw new InvalidOperationException("Passkeys:ServerDomain must be configured outside Development.");
  }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseCosmos(connectionString, databaseName));

builder.Services.Configure<IdentityPasskeyOptions>(options =>
{
  options.ServerDomain = passkeyServerDomain;
  options.AuthenticatorTimeout = TimeSpan.FromMinutes(3);
  options.ChallengeSize = 32;
});

builder.Services
    .AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(options =>
    {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = true;
  }, TimeSpan.FromHours(8), slidingExpiration: true)
    .AddDefaultTokenProviders()
    .AddDefaultUI();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
```

### 5. Ensure the database and containers exist

```csharp
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  app.UseHsts();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.EnsureCreatedAsync();
}
```

### 6. Configure the ASP.NET Core pipeline

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapDefaultControllerRoute();

app.Run();
```

## Passkey Support

The package includes passkey support for .NET 10 ASP.NET Core Identity.

### Register the passkey endpoints and client script

```csharp
builder.Services.AddCosmosPasskeyUiIntegration(options =>
{
    options.RoutePrefix = "/identity/passkeys";
    options.ClientScriptPath = "/identity/passkeys/client.js";
    options.RequireAntiforgery = true;
    options.MaxPasskeysPerUser = 100;
    options.MaxPasskeyNameLength = 200;
});

app.MapCosmosPasskeyUiEndpoints<IdentityUser>();
```

### Scaffold the passkey UI pages

Install the templates package:

```powershell
dotnet new install AspNetCore.Identity.CosmosDb.Templates
```

Then scaffold the pages:

```powershell
dotnet new cosmos-passkeys-login --RootNamespace MyApp --UserType IdentityUser
dotnet new cosmos-passkeys-ui --RootNamespace MyApp
```

The templates package is provider-agnostic at the page-model level and is documented in [AspNetCore.Identity.Razor.PassKeyPage/README.md](AspNetCore.Identity.Razor.PassKeyPage/README.md).

## Demo Website

The repository includes [AspNetCore.Identity.CosmosDb.Demo](AspNetCore.Identity.CosmosDb.Demo), a runnable example that shows how to configure the package, provision Cosmos DB, and use the passkey integration end to end.

If you want the demo **without cloning this repository**, two options are available:

- Install and scaffold the full demo app template package:

```powershell
dotnet new install AspNetCore.Identity.CosmosDb.Demo.Template
dotnet new cosmos-identity-demo -n MyIdentityCosmosDemo
```

- Download `AspNetCore.Identity.CosmosDb.Demo-source.zip` from the latest GitHub Release assets (created by [.github/workflows/demo-package.yml](.github/workflows/demo-package.yml)).

The demo README includes setup instructions, API usage notes, and page screenshots for the home, login, and passkey management flows: [AspNetCore.Identity.CosmosDb.Demo/README.md](AspNetCore.Identity.CosmosDb.Demo/README.md).

### Demo Screenshots

#### Home (`HomeController.Index()` -> `Views/Home/Index.cshtml`)

![Home page screenshot](AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Index_cshtml.png)

#### Login (`Areas/Identity/Pages/Account/Login.cshtml`)

![Login page screenshot](AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Login_cshtml.png)

#### Passkeys (`Pages/Passkeys.cshtml`)

![Passkeys page screenshot](AspNetCore.Identity.CosmosDb.Demo/wwwroot/images/Passkeys_cshtml.png)

## Backward Compatibility

Older EF Core Cosmos databases can be read by constructing the context with `backwardCompatibility: true` when needed.

```csharp
using var dbContext = new ApplicationDbContext(optionsBuilder.Options, backwardCompatibility: true);
```

This is primarily for databases created before EF Core Cosmos discriminator behavior changed in EF Core 9.

## Additional Documentation

- [AspNetCore.Identity.CosmosDb/README.md](AspNetCore.Identity.CosmosDb/README.md)
- [AspNetCore.Identity.CosmosDb.Demo/README.md](AspNetCore.Identity.CosmosDb.Demo/README.md)
- [AspNetCore.Identity.CosmosDb.Demo.Template/README.md](AspNetCore.Identity.CosmosDb.Demo.Template/README.md)
- [AspNetCore.Identity.CosmosDb.Tests/README.md](AspNetCore.Identity.CosmosDb.Tests/README.md)
- [AspNetCore.Identity.CosmosDbCompat.Tests/README.md](AspNetCore.Identity.CosmosDbCompat.Tests/README.md)
- [PASSKEY_DEVELOPER_GUIDE.md](PASSKEY_DEVELOPER_GUIDE.md)
- [PASSKEY_IMPLEMENTATION_STATUS.md](PASSKEY_IMPLEMENTATION_STATUS.md)
- [AspNetCore.Identity.Razor.PassKeyPage/README.md](AspNetCore.Identity.Razor.PassKeyPage/README.md)

## License

[MIT](LICENSE)
