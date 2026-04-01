# AspNetCore.Identity.CosmosDb .NET 10 Upgrade Tasks

## Overview

This document tracks the execution of upgrading the AspNetCore.Identity.CosmosDb solution from .NET 9.0 to .NET 10.0 (LTS). All 3 projects will be upgraded simultaneously in a single atomic operation, followed by comprehensive testing and validation.

**Progress**: 1/4 tasks complete (25%) ![0%](https://progress-bar.xyz/25)

---

## Tasks

### [✓] TASK-001: Verify prerequisites *(Completed: 2026-04-01 11:35)*
**References**: Plan §Implementation Timeline Phase 0

- [✓] (1) Verify .NET 10 SDK installed per Plan §Prerequisites: `dotnet --list-sdks`
- [✓] (2) SDK version is 10.0.104 or higher (**Verify**)

---

### [✗] TASK-002: Atomic framework and dependency upgrade with compilation fixes
**References**: Plan §Implementation Timeline Phase 1, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [✓] (1) Update `<TargetFramework>` to `net10.0` in all 3 project files per Plan §Implementation Timeline Phase 1
- [✓] (2) All project files updated to net10.0 (**Verify**)
- [✓] (3) Update all 7 package references across all projects per Plan §Package Update Reference (Microsoft.AspNetCore.Identity.EntityFrameworkCore 10.0.5, Microsoft.AspNetCore.Identity.UI 10.0.5, Microsoft.EntityFrameworkCore.Cosmos 10.0.5, Microsoft.Extensions.Caching.Memory 10.0.5, Microsoft.Extensions.Configuration.UserSecrets 10.0.5, System.Text.Json 10.0.5, Newtonsoft.Json 13.0.4)
- [✓] (4) All package references updated (**Verify**)
- [✓] (5) Restore all dependencies: `dotnet restore AspNetCore.Identity.CosmosDb.sln --force-evaluate`
- [✓] (6) All dependencies restored successfully (**Verify**)
- [✓] (7) Build solution and fix all compilation errors per Plan §Breaking Changes Catalog (fix 5 TimeSpan overload ambiguities by adding explicit `.0` suffix to integer literals or cast to `double`)
- [✓] (8) Solution builds with 0 errors (**Verify**)
- [✗] (9) Solution builds with 0 warnings (**Verify**)

---

### [ ] TASK-003: Run full test suite and validate upgrade
**References**: Plan §Implementation Timeline Phase 2, Plan §Testing & Validation Strategy

- [ ] (1) Run all test projects: `dotnet test AspNetCore.Identity.CosmosDb.sln`
- [ ] (2) Fix any test failures (reference Plan §Breaking Changes Catalog for TimeSpan-related issues or other .NET 10 behavioral changes)
- [ ] (3) Re-run tests after fixes
- [ ] (4) All tests pass with 0 failures (**Verify**)

---

### [ ] TASK-004: Final commit
**References**: Plan §Source Control Strategy

- [ ] (1) Commit all changes with message: "Upgrade solution to .NET 10.0 - Update all 3 projects to target net10.0 - Update 7 NuGet packages to .NET 10 versions - Fix 5 TimeSpan overload ambiguities - Verify all tests pass"

---



