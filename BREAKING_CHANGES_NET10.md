# .NET 10 + Passkey Upgrade Summary (Breaking Changes)

This document summarizes the important repository changes made after commit `5b0d36f01f7382c4bfeb597f20beac7b71193228` (the pre-.NET 10 baseline), including work completed for issue [#49](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/issues/49).

## Why this is a breaking release

- **Target framework baseline is now `.NET 10`** for the main package and active test/demo projects.
- **ASP.NET Core Identity passkey support (WebAuthn/FIDO2)** is now implemented in the provider and integrated into the test/demo/template surface.
- **Versioning changed to SemVer 2.0 style** and no longer mirrors the target .NET version, so version numbers may jump even when framework naming differs.

## Major changes in this range

### 1) Platform and dependency upgrade
- Main library moved to `net10.0`.
- Identity/EF Core package dependencies upgraded to the .NET 10 family.
- Active tests migrated to the consolidated `AspNetCore.Identity.CosmosDb.Tests` project.

### 2) Passkey support added end-to-end
- `CosmosUserStore` now implements `IUserPasskeyStore<TUser>`.
- New passkey entity mapping added via `UserPasskeyEntityTypeConfiguration` (Cosmos container: `Identity_Passkeys`, partitioned by `UserId`).
- New reusable passkey API + JS integration added:
  - `AddCosmosPasskeyUiIntegration(...)`
  - `MapCosmosPasskeyUiEndpoints<TUser>()`
  - Embedded script: `Passkeys/identity-passkeys.js`

### 3) New templates, demo, and docs for passkeys
- Added runnable demo app: `AspNetCore.Identity.CosmosDb.Demo`.
- Added full demo `dotnet new` template package.
- Added Razor passkey page template project.
- Added passkey implementation/readiness/developer docs.

### 4) CI, testing, and reliability updates
- CI workflows were consolidated and updated (including Windows + Cosmos Emulator setup).
- Coverage/reporting workflow behavior updated.
- Expanded passkey and store test coverage, including stress/reliability tests.

### 5) Versioning and release process updates
- Shared repo/package version now centralized (`Directory.Build.props`, current `RepoVersion` in this branch is `12.0.1`).
- Release documentation updated to reflect SemVer-based package tagging and publishing.

## Migration notes for consumers

1. Upgrade application projects to `.NET 10`.
2. Re-test Identity flows end-to-end (login, registration, claims, roles, external logins, token flows).
3. If enabling passkeys, configure `IdentityPasskeyOptions` and wire passkey UI endpoints.
4. Review release notes/versioning assumptions if your automation expected .NET-aligned package versions.

## Release notes quick checklist

### Who is affected

- [ ] Teams upgrading from package versions built for `.NET 9` or earlier.
- [ ] Teams with CI/CD or dependency policies pinned to pre-`.NET 10` TFMs.
- [ ] Teams with release automation that assumed package versions tracked the .NET major version.
- [ ] Teams adopting or extending passkey authentication flows.

### Required actions

- [ ] Retarget application and test projects to `.NET 10`.
- [ ] Upgrade package references and restore dependencies.
- [ ] Validate Identity flows in a staging environment (user CRUD, sign-in, roles, claims, tokens, external logins).
- [ ] If using passkeys, configure `IdentityPasskeyOptions`, map passkey endpoints, and validate registration/sign-in/removal flows.
- [ ] Update release automation/version checks to SemVer-based expectations.

## Reference links

- Issue: [#49 Add Support for .NET 10](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/issues/49)
- Baseline commit: [`5b0d36f`](https://github.com/CWALabs/AspNetCore.Identity.CosmosDb/commit/5b0d36f01f7382c4bfeb597f20beac7b71193228)
