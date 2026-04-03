# AspNetCore.Identity.CosmosDb.Demo.Template

`AspNetCore.Identity.CosmosDb.Demo.Template` is a `dotnet new` project template package that scaffolds a complete ASP.NET Core demo app wired for:

- ASP.NET Core Identity
- Azure Cosmos DB via `AspNetCore.Identity.CosmosDb`
- Passkey (WebAuthn) endpoints and UI

## Versioning Note

Package versions for this template now follow SemVer 2.0 and no longer track the target .NET version number.

If you notice a version jump (for example to `12.0.0`), that reflects the versioning scheme change for NuGet consistency, not a direct change in target framework by itself.

## Install

```powershell
dotnet new install AspNetCore.Identity.CosmosDb.Demo.Template
```

## Create a Demo Project

```powershell
dotnet new cosmos-identity-demo -n MyIdentityCosmosDemo
cd MyIdentityCosmosDemo
dotnet run
```

## Configure Cosmos DB

After scaffolding, set your connection values in `appsettings.json`:

- `ConnectionStrings:CosmosDb`
- `CosmosDb:DatabaseName`
- `Passkeys:ServerDomain`

For local development, use the Cosmos DB Emulator.

## Related Packages

- Main package: [AspNetCore.Identity.CosmosDb](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)
- Passkey page item templates: [AspNetCore.Identity.CosmosDb.Templates](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb.Templates)

## License

MIT
