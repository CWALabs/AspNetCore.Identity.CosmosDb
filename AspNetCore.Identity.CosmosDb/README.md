# AspNetCore.Identity.CosmosDb — Library Project

This folder contains the source code for the `AspNetCore.Identity.CosmosDb` NuGet package (.NET 10). It implements the full [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity) store interface backed by [Azure Cosmos DB](https://learn.microsoft.com/azure/cosmos-db/) via [EF Core Cosmos](https://learn.microsoft.com/ef/core/providers/cosmos/).

[![Build and Test](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml/badge.svg)](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/actions/workflows/ci.yml)
[![Coverage](https://codecov.io/gh/CWALabs/AspNetCore.Identity.CosmosDb/branch/main/graph/badge.svg)](https://codecov.io/gh/CWALabs/AspNetCore.Identity.CosmosDb)

- **NuGet package**: [`AspNetCore.Identity.CosmosDb`](https://www.nuget.org/packages/AspNetCore.Identity.CosmosDb)
- **Package README** (NuGet.org-facing): [PACKAGE_README.md](PACKAGE_README.md)
- **Repository README** (this repo): [../README.md](../README.md)

---

## Project Structure

```text
AspNetCore.Identity.CosmosDb/
├── AspNetCore.Identity.CosmosDb.csproj
├── CosmosIdentityDbContext.cs          # EF Core DbContext — model configuration entry point
├── PersonalDataConverter.cs            # Optional personal-data encryption converter
├── Retry.cs                            # Retry helper used by tests and setup utilities
├── Utilities.cs                        # Misc internal helpers
│
├── Containers/
│   └── ContainerUtilities.cs           # Low-level Cosmos SDK wrapper for DB/container provisioning
│
├── Contracts/
│   └── IRepository.cs                  # Repository abstraction consumed by the stores
│
├── EntityConfigurations/               # One IEntityTypeConfiguration<T> per Identity entity
│   ├── UserEntityTypeConfiguration.cs
│   ├── RoleEntityTypeConfiguration.cs
│   ├── UserPasskeyEntityTypeConfiguration.cs
│   └── ...
│
├── Extensions/
│   ├── ServiceCollectionExtensions.cs  # AddCosmosIdentity<>() DI registration
│   ├── PasskeyUiIntegrationExtensions.cs # AddCosmosPasskeyUiIntegration() + MapCosmosPasskeyUiEndpoints<>()
│   └── ModelBuilderExtensions.cs       # ApplyIdentityMappings<>() — wires all entity configurations
│
├── Passkeys/
│   ├── identity-passkeys.js            # Embedded client-side script (served via /identity/passkeys/client.js)
│   ├── PasskeyUiContracts.cs           # JSON request/response DTOs for passkey API endpoints
│   └── PasskeyUiIntegrationOptions.cs  # Options: route prefix, antiforgery, limits
│
├── Repositories/
│   └── CosmosIdentityRepository.cs     # IRepository implementation over CosmosIdentityDbContext
│
└── Stores/
    ├── IdentityStoreBase.cs            # Abstract base: dispose guard, exception mapping
    ├── CosmosUserStore.cs              # IUserStore + all optional user store interfaces
    └── CosmosRoleStore.cs              # IRoleStore + IQueryableRoleStore
```

---

## Key Public API

### DI Registration

```csharp
// Full registration — Cosmos DbContext + stores + cookie auth
services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(options =>
{
    options.Password.RequireDigit = true;
});

// With custom cookie lifetime
services.AddCosmosIdentity<ApplicationDbContext, IdentityUser, IdentityRole, string>(
    options => { ... },
    cookieExpireTimeSpan: TimeSpan.FromHours(8),
    slidingExpiration: true);
```

### DbContext

```csharp
public class ApplicationDbContext
    : CosmosIdentityDbContext<IdentityUser, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions options) : base(options) { }
}
```

`CosmosIdentityDbContext` overrides `OnModelCreating` to:

1. Call `builder.HasDiscriminatorInJsonIds()` — required for EF Core 9+ Cosmos discriminator behavior.
2. Apply all `EntityConfigurations/` via `ModelBuilderExtensions.ApplyIdentityMappings<>()`.
3. Optionally call `builder.HasEmbeddedDiscriminatorName("Discriminator")` when `backwardCompatibility: true` is passed to the constructor (see [Backward Compatibility](#backward-compatibility)).

### Passkey UI Integration

```csharp
// Program.cs
builder.Services.AddCosmosPasskeyUiIntegration(options =>
{
    options.RoutePrefix = "/identity/passkeys"; // default
    options.RequireAntiforgery = true;          // default
    options.MaxPasskeysPerUser = 100;           // default
});

app.MapCosmosPasskeyUiEndpoints<IdentityUser>();
```

`MapCosmosPasskeyUiEndpoints<TUser>()` registers these endpoints under the configured route prefix:

| Method | Path | Purpose |
| --- | --- | --- |
| `GET` | `/client.js` | Serves the embedded `identity-passkeys.js` client script |
| `GET` | `/list` | Returns the signed-in user's registered passkeys |
| `POST` | `/register/options` | Creates a WebAuthn registration challenge |
| `POST` | `/register/complete` | Finalises credential registration |
| `POST` | `/authenticate/options` | Creates a WebAuthn authentication challenge |
| `POST` | `/authenticate/complete` | Verifies a passkey assertion and signs the user in |
| `POST` | `/remove` | Removes a specific passkey by credential ID |
| `POST` | `/rename` | Renames a passkey |

---

## Stores

### `CosmosUserStore<TUserEntity, TRoleEntity, TKey>`

Implements every optional Identity user store interface:

| Interface | Capability |
| --- | --- |
| `IUserStore<T>` | Core CRUD |
| `IUserRoleStore<T>` | Role assignment |
| `IUserEmailStore<T>` | Email address |
| `IUserPasswordStore<T>` | Password hash |
| `IUserPhoneNumberStore<T>` | Phone number |
| `IUserLockoutStore<T>` | Lockout / failed access tracking |
| `IUserClaimStore<T>` | Claims |
| `IUserSecurityStampStore<T>` | Security stamp |
| `IUserTwoFactorStore<T>` | Two-factor enabled flag |
| `IUserTwoFactorRecoveryCodeStore<T>` | Recovery codes |
| `IUserLoginStore<T>` | External logins |
| `IUserAuthenticatorKeyStore<T>` | TOTP authenticator key |
| `IQueryableUserStore<T>` | LINQ queries over users |
| `IUserPasskeyStore<T>` | Passkey credential storage |

Passkey operations use per-user `SemaphoreSlim` instances (keyed on `TKey`) to prevent concurrent modification of a user's passkey collection.

### `CosmosRoleStore<TRoleEntity, TKey>`

Implements `IRoleStore<T>`, `IQueryableRoleStore<T>`, and `IRoleClaimStore<T>`.

---

## Entity Configurations

Each Identity entity type has its own `IEntityTypeConfiguration<T>` under `EntityConfigurations/`. Key decisions:

- Every entity is explicitly configured for Cosmos DB (no relational column/index defaults).
- `UserPasskeyEntityTypeConfiguration` maps passkey credentials as owned entities embedded in the user document.
- `DeviceFlowCodesEntityTypeConfiguration` and `PersistedGrantEntityTypeConfiguration` support [Duende IdentityServer](https://duendesoftware.com/products/identityserver) when the optional dependency is used.

---

## Repository Layer

`CosmosIdentityRepository<TDbContext, TUser, TRole, TKey>` wraps `CosmosIdentityDbContext` and implements `IRepository`. The stores only ever call `IRepository` — this indirection makes the stores testable with a Moq-based mock without a live Cosmos DB connection.

> Synchronous members on `IRepository` (`GetById`, `TryFindOne`, `DeleteById`) are marked `[Obsolete]`. Prefer their `Async` equivalents for all Cosmos-backed execution paths.

---

## Backward Compatibility

Databases created with versions prior to EF Core 9 used `"Discriminator"` as the embedded type field name. EF Core 9 changed the default to `"$type"`. Pass `backwardCompatibility: true` when constructing the `DbContext` to restore the old field name and read those databases:

```csharp
var options = new DbContextOptionsBuilder()
    .UseCosmos(connectionString, databaseName)
    .Options;

using var db = new ApplicationDbContext(options, backwardCompatibility: true);
```

---

## Key Dependencies

| Package | Purpose |
| --- | --- |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Base Identity EF Core classes and interfaces |
| `Microsoft.EntityFrameworkCore.Cosmos` | EF Core Cosmos DB provider |
| `Microsoft.AspNetCore.Identity.UI` | Razor Pages Identity UI scaffolding support |
| `Duende.IdentityServer.EntityFramework.Storage` | `DeviceFlowCodes` / `PersistedGrant` entity support |
| `Microsoft.Extensions.Caching.Memory` | In-process caching used by store operations |
| `Newtonsoft.Json` | JSON serialisation helpers |
| `Microsoft.SourceLink.GitHub` | Source Link for debugger step-into support |

---

## Related Projects

| Project | Description |
| --- | --- |
| [`AspNetCore.Identity.CosmosDb.Tests`](../AspNetCore.Identity.CosmosDb.Tests) | Primary test suite |
| [`AspNetCore.Identity.CosmosDbCompat.Tests`](../AspNetCore.Identity.CosmosDbCompat.Tests) | API compatibility regression suite |
| [`AspNetCore.Identity.CosmosDb.Demo.Template`](../AspNetCore.Identity.CosmosDb.Demo.Template) | `dotnet new` full demo app template package |
| [`AspNetCore.Identity.Razor.PassKeyPage`](../AspNetCore.Identity.Razor.PassKeyPage) | `dotnet new` Razor Pages passkey UI templates |
| [`AspNetCore.Identity.CosmosDb.Demo`](../AspNetCore.Identity.CosmosDb.Demo) | Runnable demo web application |
