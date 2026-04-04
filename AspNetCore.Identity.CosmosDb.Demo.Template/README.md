# CWALabs.AspNetCore.Identity.CosmosDb.DemoTemplate

`CWALabs.AspNetCore.Identity.CosmosDb.DemoTemplate` is a `dotnet new` project template package that scaffolds a complete ASP.NET Core demo app wired for:

[Jump to install instructions](#install)

- ASP.NET Core Identity
- Azure Cosmos DB via `AspNetCore.Identity.CosmosDb`
- Passkey (WebAuthn) endpoints and UI

## Versioning Note

Package versions for this template now follow SemVer 2.0 and no longer track the target .NET version number.

If you notice a version jump (for example to `12.0.0`), that reflects the versioning scheme change for NuGet consistency, not a direct change in target framework by itself.

## Install

```powershell
dotnet new install CWALabs.AspNetCore.Identity.CosmosDb.DemoTemplate
```

What this does:

- `dotnet new install` tells the .NET SDK to download and register a template package on your machine.
- `CWALabs.AspNetCore.Identity.CosmosDb.DemoTemplate` is the NuGet package ID for this template pack.
- After installation, the template becomes available to the `dotnet new` command, just like the built-in project templates that ship with the SDK.
- This does not create a project yet. It only makes the template available for later use.

If you want to see which template packs are installed locally, run:

```powershell
dotnet new uninstall
```

If you later want to remove this template pack, run:

```powershell
dotnet new uninstall CWALabs.AspNetCore.Identity.CosmosDb.DemoTemplate
```

Official documentation:

- [`dotnet new` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new)
- [`dotnet new install` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-install)
- [Custom templates for `dotnet new`](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates)

## Create a Demo Project

```powershell
dotnet new cosmos-identity-demo -n MyIdentityCosmosDemo
cd MyIdentityCosmosDemo
dotnet run
```

What this does:

- `dotnet new cosmos-identity-demo` creates a new project from the installed template.
- `cosmos-identity-demo` is the template short name. It is the friendly command name exposed by the template package after installation.
- `-n MyIdentityCosmosDemo` sets the name of the generated project and the output folder name.
- `cd MyIdentityCosmosDemo` moves into the newly created project directory.
- `dotnet run` restores dependencies if needed, builds the app, and starts it locally.

If you want to create the project in a different folder, you can also use `-o`:

```powershell
dotnet new cosmos-identity-demo -n MyIdentityCosmosDemo -o .\samples\MyIdentityCosmosDemo
```

If you want to see the template's available options before creating a project, run:

```powershell
dotnet new cosmos-identity-demo --help
```

Official documentation:

- [Create a project with `dotnet new`](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new)
- [Create a project using a custom template](https://learn.microsoft.com/en-us/dotnet/core/tools/custom-templates#create-a-project-using-a-custom-template)
- [`dotnet run` command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-run)

## Configure Cosmos DB

After scaffolding, set your connection values in `appsettings.json`:

- `ConnectionStrings:CosmosDb`
- `CosmosDb:DatabaseName`
- `Passkeys:ServerDomain`

For local development, use the Cosmos DB Emulator.

## Related Packages

- Main package: [AspNetCore.Identity.CosmosDb](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)
- Passkey page item templates: [CWALabs.AspNetCore.Identity.CosmosDb.PasskeyTemplates](https://www.nuget.org/packages/CWALabs.AspNetCore.Identity.CosmosDb.PasskeyTemplates)

## License

MIT
