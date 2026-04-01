# .NET 10 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
  - [AspNetCore.Identity.CosmosDb](#aspnetcoreidentitycosmosdb)
  - [AspNetCore.Identity.CosmosDbCompat.Tests](#aspnetcoreidentitycosmosdbcompattests)
  - [AspNetCore.Identity.CosmosDb.Tests](#aspnetcoreidentityCosmosDbtests)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Risk Management](#risk-management)
- [Testing & Validation Strategy](#testing--validation-strategy)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)
- [Secondary Breaking-Change Checklist (.NET 10 / ASP.NET Core 10)](#secondary-breaking-change-checklist-net-10--aspnet-core-10)
- [Passkey Implementation Checklist (EF Cosmos Compatible)](#passkey-implementation-checklist-ef-cosmos-compatible)
- [EF Core 10 Cosmos Compatibility Checklist](#ef-core-10-cosmos-compatibility-checklist)

---

## Executive Summary

### Scenario Description
This plan outlines the upgrade of the **AspNetCore.Identity.CosmosDb** solution from **.NET 9.0** to **.NET 10.0 (Long Term Support)**. The solution consists of 3 projects: one core class library and two test projects.

### Scope
**Projects Affected**: 3 of 3 projects
- **AspNetCore.Identity.CosmosDb.csproj** (ClassLibrary) - Core library, dependency for test projects
- **AspNetCore.Identity.CosmosDbCompat.Tests.csproj** (DotNetCoreApp) - Compatibility test project
- **AspNetCore.Identity.CosmosDb.Tests.csproj** (DotNetCoreApp) - V9-specific test project

**Current State**: All projects target .NET 9.0

**Target State**: All projects target .NET 10.0

### Selected Strategy
**All-At-Once Strategy** - All projects upgraded simultaneously in a single operation.

**Rationale**:
- Small solution (3 projects)
- All currently on .NET 9.0
- Clear, simple dependency structure (library + 2 test projects)
- All packages have .NET 10 compatible versions available
- Low complexity across all projects
- No security vulnerabilities requiring immediate attention

### Complexity Assessment
**Discovered Metrics**:
- **Total Projects**: 3
- **Total NuGet Packages**: 16 (7 require updates)
- **Total Code Files**: 35
- **Files with Incidents**: 8
- **Total Lines of Code**: 7,462
- **Estimated LOC to Modify**: 5+ (0.1% of codebase)
- **API Issues**: 5 source-incompatible APIs (all TimeSpan-related)
- **Dependency Depth**: 2 levels
- **Security Vulnerabilities**: 0

**Classification**: **Simple Solution**

**Critical Issues**: None - no security vulnerabilities, no binary-incompatible APIs, no blocking issues

### Recommended Approach
**All-At-Once Atomic Upgrade**: Update all project files, package references, and fix compilation errors in a single coordinated operation, followed by comprehensive testing.

### Iteration Strategy Used
Fast batch approach - Simple solution allows grouping all projects for efficient planning with 2-3 detail iterations.

## Migration Strategy

### Approach Selection

**Selected Strategy: All-At-Once**

All 3 projects will be upgraded simultaneously in a single atomic operation.

### Justification

**Why All-At-Once is Ideal**:

1. **Small Solution Size**: 3 projects falls well within the All-At-Once recommended range (<5 projects)

2. **Homogeneous Codebase**: 
   - All projects currently on .NET 9.0
   - All projects targeting .NET 10.0
   - Consistent technology stack (ASP.NET Core Identity, Entity Framework Core, Cosmos DB)

3. **Simple Dependencies**: 
   - Only 2 levels deep
   - Clear tree structure (no cycles)
   - Test projects are independent of each other

4. **Low External Complexity**:
   - All 7 packages requiring updates have known .NET 10 versions
   - No incompatible packages
   - No packages with breaking changes requiring special handling

5. **Assessment Confirmation**:
   - All projects marked as "Low" difficulty
   - No security vulnerabilities
   - Minimal API incompatibilities (5 source-incompatible APIs, all TimeSpan overload selection)

**All-At-Once Strategy Rationale**:
- **Fastest completion**: Single coordinated update minimizes total migration time
- **No multi-targeting complexity**: Avoid interim states where some projects target .NET 9 while others target .NET 10
- **Clean dependency resolution**: All projects resolve packages against .NET 10 simultaneously
- **Simple coordination**: Small team can coordinate single upgrade window
- **Unified testing**: Test entire solution in .NET 10 state at once

### Dependency-Based Ordering

While all projects are updated simultaneously, the natural dependency order is respected during compilation and error resolution:

1. **AspNetCore.Identity.CosmosDb.csproj** - Foundation library (no dependencies)
   - Must compile successfully first
   - Fixes here may impact dependent projects

2. **Test Projects** (can be addressed in parallel after core library builds)
   - AspNetCore.Identity.CosmosDbCompat.Tests.csproj
   - AspNetCore.Identity.CosmosDb.Tests.csproj

**Ordering Principle**: Even in an atomic upgrade, compilation errors in the core library take priority over test project errors, as test projects cannot build until their dependency builds.

### Execution Approach

**Single Coordinated Operation**:

All updates happen together:
- Update all 3 project files (TargetFramework: net9.0 → net10.0)
- Update all 7 package references across all projects
- Restore dependencies for entire solution
- Build solution and fix all compilation errors
- Solution builds with 0 errors

**No Intermediate States**: The solution moves directly from "fully .NET 9" to "fully .NET 10" without interim multi-targeting or phased rollout.

### Advantages for This Solution

1. **Speed**: Complete upgrade in single session
2. **Simplicity**: No complex phasing or multi-targeting
3. **Clean Testing**: Test suite validates entire .NET 10 solution
4. **Minimal Coordination**: All developers adopt .NET 10 simultaneously
5. **Single Deployment**: No staggered release concerns

### Risk Management

**Mitigated by**:
- Low complexity assessment across all projects
- No breaking changes in package updates
- Good test coverage (2 test projects with 4,832 LOC of tests)
- Clean rollback via Git branch (already on `upgrade-to-NET10` branch)

## Detailed Dependency Analysis

### Dependency Graph Summary

The solution has a simple, two-tier dependency structure:

```
Tier 1 (Foundation):
└── AspNetCore.Identity.CosmosDb.csproj (Core Library)
    └── 0 project dependencies

Tier 2 (Consumers):
├── AspNetCore.Identity.CosmosDbCompat.Tests.csproj
│   └── Depends on: AspNetCore.Identity.CosmosDb.csproj
└── AspNetCore.Identity.CosmosDb.Tests.csproj
    └── Depends on: AspNetCore.Identity.CosmosDb.csproj
```

### Project Groupings by Migration Phase

**Single Atomic Phase**: All projects upgraded simultaneously

All 3 projects will be updated in one coordinated operation:
1. **AspNetCore.Identity.CosmosDb.csproj** - Foundation library
2. **AspNetCore.Identity.CosmosDbCompat.Tests.csproj** - Test project
3. **AspNetCore.Identity.CosmosDb.Tests.csproj** - Test project

**Rationale**: 
- Simple dependency structure allows safe simultaneous upgrade
- Both test projects depend only on the core library
- No circular dependencies
- All projects on same framework version (net9.0)
- Package compatibility verified for all projects

### Critical Path Identification

**Critical Path**: AspNetCore.Identity.CosmosDb.csproj → Test Projects

The core library is the foundation. While the All-At-Once strategy updates all projects simultaneously, any compilation errors in the core library must be resolved before test projects can build successfully.

**Key Dependencies**:
- Test projects reference the core library via `<ProjectReference>`
- Both test projects are independent of each other (no cross-dependency)
- Core library has no project dependencies

### Circular Dependencies

**None detected**. The dependency graph is a clean tree structure with no cycles.

## Project-by-Project Plans

All projects are upgraded simultaneously as part of the All-At-Once strategy. The following plans provide detailed specifications for each project's transformation.

### AspNetCore.Identity.CosmosDb

**Current State**:
- Target Framework: net9.0
- Project Type: ClassLibrary (SDK-style)
- Dependencies: 0 project dependencies
- Dependants: 2 (both test projects)
- Package Count: 7 NuGet packages
- Lines of Code: 2,630
- Files: 23 (4 files with incidents)
- Risk Level: Low

**Target State**:
- Target Framework: net10.0
- Updated Packages: 6 packages

#### Migration Steps

**1. Prerequisites**
- Verify .NET 10 SDK installed: `dotnet --list-sdks` (should show 10.0.104 or higher)
- Ensure on `upgrade-to-NET10` branch: `git branch --show-current`

**2. Framework Update**

Update `AspNetCore.Identity.CosmosDb\AspNetCore.Identity.CosmosDb.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

**3. Package Updates**

| Package | Current Version | Target Version | Reason |
|:---|:---:|:---:|:---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.AspNetCore.Identity.UI | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.EntityFrameworkCore.Cosmos | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.Extensions.Caching.Memory | 9.0.8 | 10.0.5 | Framework compatibility |
| System.Text.Json | 9.0.8 | 10.0.5 | Framework compatibility |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | Recommended update |

**Packages Remaining Unchanged**:
- Duende.IdentityServer.EntityFramework.Storage 7.3.1 (already compatible)

**4. Expected Breaking Changes**

**Source Incompatibilities** (3 occurrences):

**TimeSpan.FromSeconds(long) Ambiguity**:
- **Issue**: `TimeSpan.FromSeconds(int)` calls may become ambiguous due to new overloads accepting `long` in .NET 10
- **Locations**: 3 occurrences in the codebase (specific files identified during compilation)
- **Fix**: Add explicit cast to `double`:
  ```csharp
  // Before
  TimeSpan.FromSeconds(60)

  // After
  TimeSpan.FromSeconds(60.0)  // or TimeSpan.FromSeconds((double)60)
  ```

**No Binary Incompatibilities**: All breaking changes are source-level only (caught at compile time).

**5. Code Modifications**

**Areas Requiring Review**:
- **Identity Store Implementation**: Verify no behavior changes in ASP.NET Core Identity 10.0
- **Cosmos DB EF Provider**: Review EF Core 10.0 Cosmos provider changes (if any)
- **Caching Logic**: Verify MemoryCache behavior consistency

**Expected Changes**:
- Fix TimeSpan overload ambiguities (3 locations)
- No configuration changes expected
- No namespace changes expected

**6. Testing Strategy**

**Unit Tests** (via dependent test projects):
- AspNetCore.Identity.CosmosDbCompat.Tests.csproj will validate compatibility scenarios
- AspNetCore.Identity.CosmosDb.Tests.csproj will validate V9-specific scenarios

**Expected Test Coverage**:
- User store operations (CRUD)
- Role store operations (CRUD)
- Claims management
- Token management
- Cosmos DB integration

**7. Validation Checklist**

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] No package dependency conflicts
- [ ] Dependent test projects build successfully
- [ ] All tests in dependent projects pass
- [ ] No performance regressions observed

---

### AspNetCore.Identity.CosmosDbCompat.Tests

**Current State**:
- Target Framework: net9.0
- Project Type: DotNetCoreApp (SDK-style)
- Dependencies: 1 (AspNetCore.Identity.CosmosDb.csproj)
- Dependants: 0
- Package Count: 6 NuGet packages
- Lines of Code: 1,818
- Files: 7 (2 files with incidents)
- Risk Level: Low

**Target State**:
- Target Framework: net10.0
- Updated Packages: 1 package

#### Migration Steps

**1. Prerequisites**
- AspNetCore.Identity.CosmosDb.csproj must build successfully first (dependency requirement)

**2. Framework Update**

Update `AspNetCore.Identity.CosmosDbCompat.Tests\AspNetCore.Identity.CosmosDbCompat.Tests.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

**3. Package Updates**

| Package | Current Version | Target Version | Reason |
|:---|:---:|:---:|:---|
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.8 | 10.0.5 | Framework compatibility |

**Packages Remaining Unchanged** (already compatible):
- Microsoft.NET.Test.Sdk 17.14.1
- Microsoft.Testing.Extensions.CodeCoverage 17.14.2
- Microsoft.Testing.Extensions.TrxReport 1.8.4
- Moq 4.20.72
- MSTest 3.10.4

**4. Expected Breaking Changes**

**Source Incompatibilities** (1 occurrence):

**TimeSpan Method Overload**:
- **Issue**: Similar to core library, TimeSpan method call may need explicit type
- **Fix**: Add explicit cast to `double` where needed

**5. Code Modifications**

**Areas Requiring Review**:
- Test setup/teardown code using TimeSpan values
- Configuration loading (UserSecrets compatibility)

**Expected Changes**:
- Fix TimeSpan overload ambiguity (1 location)
- Verify MSTest framework compatibility (no changes expected)

**6. Testing Strategy**

**Test Execution**:
- Run full compatibility test suite: `dotnet test AspNetCore.Identity.CosmosDbCompat.Tests.csproj`
- Verify all tests pass with .NET 10 runtime

**Test Categories** (based on project name):
- Compatibility scenarios with existing Identity implementations
- Backward compatibility validation

**7. Validation Checklist**

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All tests pass
- [ ] Test coverage maintained
- [ ] UserSecrets configuration loads correctly
- [ ] Cosmos DB test connections work (emulator or test instance)

---

### AspNetCore.Identity.CosmosDb.Tests

**Current State**:
- Target Framework: net9.0
- Project Type: DotNetCoreApp (SDK-style)
- Dependencies: 1 (AspNetCore.Identity.CosmosDb.csproj)
- Dependants: 0
- Package Count: 7 NuGet packages
- Lines of Code: 3,014
- Files: 11 (2 files with incidents)
- Risk Level: Low

**Target State**:
- Target Framework: net10.0
- Updated Packages: 3 packages

#### Migration Steps

**1. Prerequisites**
- AspNetCore.Identity.CosmosDb.csproj must build successfully first (dependency requirement)

**2. Framework Update**

Update `AspNetCore.Identity.CosmosDb.Tests\AspNetCore.Identity.CosmosDb.Tests.csproj`:

```xml
<TargetFramework>net10.0</TargetFramework>
```

**3. Package Updates**

| Package | Current Version | Target Version | Reason |
|:---|:---:|:---:|:---|
| Microsoft.Extensions.Caching.Memory | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.8 | 10.0.5 | Framework compatibility |
| System.Text.Json | 9.0.8 | 10.0.5 | Framework compatibility |

**Packages Remaining Unchanged** (already compatible):
- coverlet.collector 6.0.4
- Microsoft.NET.Test.Sdk 17.14.1
- Moq 4.20.72
- MSTest.TestAdapter 3.10.4
- MSTest.TestFramework 3.10.4

**4. Expected Breaking Changes**

**Source Incompatibilities** (1 occurrence):

**TimeSpan Method Overload**:
- **Issue**: TimeSpan.FromMinutes or FromSeconds call may need explicit type
- **Fix**: Add explicit cast to `double` where needed

**5. Code Modifications**

**Areas Requiring Review**:
- Test setup code using TimeSpan values
- Caching test scenarios (MemoryCache behavior)
- JSON serialization tests (System.Text.Json 10.0 changes)
- Configuration loading (UserSecrets compatibility)

**Expected Changes**:
- Fix TimeSpan overload ambiguity (1 location)
- Verify MSTest framework compatibility (no changes expected)
- Validate System.Text.Json serialization behavior (no changes expected for typical usage)

**6. Testing Strategy**

**Test Execution**:
- Run full V9-specific test suite: `dotnet test AspNetCore.Identity.CosmosDb.Tests.csproj`
- Verify all tests pass with .NET 10 runtime

**Test Categories** (based on project name):
- V9-specific functionality tests
- Latest features validation
- Performance tests (if any)

**7. Validation Checklist**

- [ ] Project builds without errors
- [ ] Project builds without warnings
- [ ] All tests pass
- [ ] Test coverage maintained
- [ ] Caching behavior consistent
- [ ] JSON serialization/deserialization works correctly
- [ ] UserSecrets configuration loads correctly
- [ ] Cosmos DB test connections work (emulator or test instance)

---

## Package Update Reference

### Summary by Scope

**Total Packages**: 16
- **Updates Required**: 7 (43.8%)
- **Already Compatible**: 9 (56.3%)
- **Incompatible**: 0 (0%)

### Common Package Updates (Affecting Multiple Projects)

| Package | Current | Target | Projects Affected | Update Reason |
|:---|:---:|:---:|:---|:---|
| Microsoft.Extensions.Caching.Memory | 9.0.8 | 10.0.5 | 2 projects:<br/>• AspNetCore.Identity.CosmosDb<br/>• AspNetCore.Identity.CosmosDb.Tests | Framework compatibility |
| Microsoft.Extensions.Configuration.UserSecrets | 9.0.8 | 10.0.5 | 2 projects:<br/>• AspNetCore.Identity.CosmosDbCompat.Tests<br/>• AspNetCore.Identity.CosmosDb.Tests | Framework compatibility |
| System.Text.Json | 9.0.8 | 10.0.5 | 2 projects:<br/>• AspNetCore.Identity.CosmosDb<br/>• AspNetCore.Identity.CosmosDb.Tests | Framework compatibility |

### Category-Specific Updates

**Core Library** (AspNetCore.Identity.CosmosDb.csproj):

| Package | Current | Target | Update Reason |
|:---|:---:|:---:|:---|
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.AspNetCore.Identity.UI | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.EntityFrameworkCore.Cosmos | 9.0.8 | 10.0.5 | Framework compatibility |
| Microsoft.Extensions.Caching.Memory | 9.0.8 | 10.0.5 | Framework compatibility |
| System.Text.Json | 9.0.8 | 10.0.5 | Framework compatibility |
| Newtonsoft.Json | 13.0.3 | 13.0.4 | Recommended update |

**Test Projects**:

*AspNetCore.Identity.CosmosDbCompat.Tests.csproj*:
- Microsoft.Extensions.Configuration.UserSecrets: 9.0.8 → 10.0.5

*AspNetCore.Identity.CosmosDb.Tests.csproj*:
- Microsoft.Extensions.Caching.Memory: 9.0.8 → 10.0.5
- Microsoft.Extensions.Configuration.UserSecrets: 9.0.8 → 10.0.5
- System.Text.Json: 9.0.8 → 10.0.5

### Packages Requiring No Updates (Already Compatible)

| Package | Version | Projects |
|:---|:---:|:---|
| coverlet.collector | 6.0.4 | AspNetCore.Identity.CosmosDb.Tests |
| Duende.IdentityServer.EntityFramework.Storage | 7.3.1 | AspNetCore.Identity.CosmosDb |
| Microsoft.NET.Test.Sdk | 17.14.1 | Both test projects |
| Microsoft.Testing.Extensions.CodeCoverage | 17.14.2 | AspNetCore.Identity.CosmosDbCompat.Tests |
| Microsoft.Testing.Extensions.TrxReport | 1.8.4 | AspNetCore.Identity.CosmosDbCompat.Tests |
| Moq | 4.20.72 | Both test projects |
| MSTest | 3.10.4 | AspNetCore.Identity.CosmosDbCompat.Tests |
| MSTest.TestAdapter | 3.10.4 | AspNetCore.Identity.CosmosDb.Tests |
| MSTest.TestFramework | 3.10.4 | AspNetCore.Identity.CosmosDb.Tests |

### Update Execution Notes

**All-At-Once Approach**:
- All 7 package updates applied simultaneously across all 3 projects
- No staged rollout needed
- Package restore validates compatibility across entire solution

**Update Method**:
```bash
# Update all packages in all projects
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package Microsoft.AspNetCore.Identity.UI --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package Microsoft.EntityFrameworkCore.Cosmos --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package Microsoft.Extensions.Caching.Memory --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package System.Text.Json --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj package Newtonsoft.Json --version 13.0.4

dotnet add AspNetCore.Identity.CosmosDbCompat.Tests/AspNetCore.Identity.CosmosDbCompat.Tests.csproj package Microsoft.Extensions.Configuration.UserSecrets --version 10.0.5

dotnet add AspNetCore.Identity.CosmosDb.Tests/AspNetCore.Identity.CosmosDb.Tests.csproj package Microsoft.Extensions.Caching.Memory --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb.Tests/AspNetCore.Identity.CosmosDb.Tests.csproj package Microsoft.Extensions.Configuration.UserSecrets --version 10.0.5
dotnet add AspNetCore.Identity.CosmosDb.Tests/AspNetCore.Identity.CosmosDb.Tests.csproj package System.Text.Json --version 10.0.5

# Or manually edit .csproj files and run restore
dotnet restore
```

**Verification**:
After updates, verify no package conflicts:
```bash
dotnet restore --force-evaluate
dotnet list package --vulnerable
dotnet list package --deprecated
```

## Breaking Changes Catalog

### Overview

All breaking changes identified are **source-level incompatibilities** that will be caught at compile time. No binary-incompatible or behavioral changes have been detected.

**Total Issues**: 5 source-incompatible API calls
**Impact**: 0.1% of codebase (5+ LOC out of 7,462)

### Framework Breaking Changes

#### TimeSpan Method Overload Resolution

**Category**: Source Incompatibility
**Severity**: Low (compile-time, straightforward fix)
**Occurrences**: 5 across all projects

**Description**:
.NET 10 introduces new overloads for `TimeSpan.FromSeconds` and `TimeSpan.FromMinutes` that accept `long` parameters. This can cause overload resolution ambiguity when passing integer literals or `int` values.

**Affected APIs**:
- `System.TimeSpan.FromSeconds(System.Int64)` - 4 occurrences
- `System.TimeSpan.FromMinutes(System.Int64)` - 1 occurrence

**Error Pattern**:
```
CS0121: The call is ambiguous between the following methods or properties:
'TimeSpan.FromSeconds(double)' and 'TimeSpan.FromSeconds(long)'
```

**Fix**:
Add explicit cast to `double` or use floating-point literal:

```csharp
// ❌ Before (ambiguous in .NET 10)
TimeSpan timeout = TimeSpan.FromSeconds(60);
TimeSpan delay = TimeSpan.FromMinutes(5);

// ✅ After (explicit type)
TimeSpan timeout = TimeSpan.FromSeconds(60.0);
TimeSpan delay = TimeSpan.FromMinutes(5.0);

// ✅ Alternative (explicit cast)
TimeSpan timeout = TimeSpan.FromSeconds((double)60);
TimeSpan delay = TimeSpan.FromMinutes((double)5);
```

**Locations**:
Exact file locations will be identified during compilation. Based on assessment:
- **AspNetCore.Identity.CosmosDb.csproj**: 3 occurrences (4 files with incidents)
- **AspNetCore.Identity.CosmosDbCompat.Tests.csproj**: 1 occurrence (2 files with incidents)
- **AspNetCore.Identity.CosmosDb.Tests.csproj**: 1 occurrence (2 files with incidents)

**Resolution Strategy**:
1. Build solution to get exact error locations
2. For each error, add `.0` to the integer literal (e.g., `60` → `60.0`)
3. Rebuild to verify fix
4. Repeat until all occurrences resolved

### Package Breaking Changes

#### Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.8 → 10.0.5)

**Category**: Minor version upgrade
**Expected Impact**: None - minor release, backward compatible

**Areas to Validate**:
- UserStore and RoleStore implementations
- Identity schema/migrations (none expected in this project - uses Cosmos DB)
- Identity configuration API

**Known Changes**: Review [ASP.NET Core 10.0 migration guide](https://learn.microsoft.com/en-us/aspnet/core/migration/9-to-10) for any identity-specific changes.

#### Microsoft.EntityFrameworkCore.Cosmos (9.0.8 → 10.0.5)

**Category**: Minor version upgrade
**Expected Impact**: None - minor release, backward compatible

**Areas to Validate**:
- Cosmos DB provider configuration
- Query translation behavior
- Change tracking behavior

**Known Changes**: Review [EF Core 10.0 breaking changes](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes) for Cosmos provider specifics.

#### System.Text.Json (9.0.8 → 10.0.5)

**Category**: Minor version upgrade
**Expected Impact**: None - minor release, backward compatible

**Areas to Validate**:
- Custom JsonConverter implementations (if any)
- Serialization/deserialization of Identity types
- JSON configuration files

**Known Changes**: Review [.NET 10 JSON changes](https://learn.microsoft.com/en-us/dotnet/core/compatibility/serialization/10.0) if serialization issues arise.

#### Other Package Updates

All other package updates (Microsoft.Extensions.*, Newtonsoft.Json) are minor version upgrades with no known breaking changes affecting this solution.

### Configuration Changes

**None Expected**:
- No appsettings.json changes required
- No middleware registration changes
- No service registration changes
- No authentication/authorization changes

### Migration Summary Table

| Change Type | Count | Severity | Resolution |
|:---|:---:|:---:|:---|
| Source Incompatible APIs | 5 | 🟡 Low | Add explicit type casts (compile-time fix) |
| Binary Incompatible APIs | 0 | N/A | N/A |
| Behavioral Changes | 0 | N/A | N/A |
| Configuration Changes | 0 | N/A | N/A |
| Package Breaking Changes | 0 | N/A | All minor version updates |

### Validation Approach

**Compile-Time Validation**:
- Build solution after framework + package updates
- Compiler will identify all 5 source incompatibilities
- Apply fixes and rebuild until 0 errors

**Runtime Validation**:
- Execute full test suite (2 test projects)
- Manual testing of Identity operations (if applicable)
- Monitor application logs for unexpected behavior

**No Unknown Breaking Changes Expected**: All changes identified by the compatibility analyzer. .NET 10 is a minor version upgrade with strong backward compatibility guarantees.

## Risk Management

### High-Level Risk Assessment

**Overall Risk Level**: **Low**

All three projects assessed as "Low" difficulty by the compatibility analyzer. The upgrade presents minimal risk due to:
- Small codebase (7,462 LOC total)
- No security vulnerabilities in current packages
- No binary-incompatible API changes
- All packages have .NET 10 compatible versions
- Good test coverage (2 test projects)

### Risk Factors by Category

| Risk Category | Level | Description | Mitigation |
|:---|:---:|:---|:---|
| **API Compatibility** | 🟢 Low | 5 source-incompatible APIs (TimeSpan overload selection) | Build will identify exact locations; straightforward fixes (add explicit casts) |
| **Package Compatibility** | 🟢 Low | All 7 packages have .NET 10 versions; 9 packages already compatible | Verify package updates during restore |
| **Dependency Conflicts** | 🟢 Low | Simple tree structure, no circular dependencies | Restore will validate; no multi-targeting needed |
| **Breaking Changes** | 🟢 Low | Only source incompatibilities (compiler can catch) | Build process exposes all issues before runtime |
| **Security** | 🟢 Low | No vulnerabilities in current or target packages | N/A - no security-driven urgency |
| **Testing** | 🟢 Low | 4,832 LOC of test code across 2 test projects | Run full test suite after upgrade |
| **Rollback** | 🟢 Low | Clean Git branch (`upgrade-to-NET10`) | Rollback via `git checkout main` |

### Project-Specific Risks

**AspNetCore.Identity.CosmosDb.csproj** (Core Library):
- **Risk**: Foundation library - errors here block test projects
- **Mitigation**: Address core library compilation errors first
- **Likelihood**: Low (only 3 source-incompatible APIs, 0.1% LOC impact)

**Test Projects**:
- **Risk**: Test failures may indicate runtime behavior changes
- **Mitigation**: Full test execution validates functional correctness
- **Likelihood**: Low (minimal API changes, consistent framework migration)

### Security Vulnerabilities

**None detected**. All current packages are secure. No security-driven package updates required.

### Contingency Plans

**If build fails after framework update**:
1. Review compilation errors - expected to be TimeSpan method overload selection (add explicit casts to `double`)
2. Check for additional API incompatibilities flagged by compiler
3. Reference .NET 10 breaking changes documentation if unexpected errors appear

**If package updates fail**:
1. Verify .NET 10 SDK is installed (10.0.104 or higher)
2. Clear NuGet caches: `dotnet nuget locals all --clear`
3. Retry restore with verbose logging: `dotnet restore -v detailed`

**If tests fail**:
1. Isolate failing tests
2. Review .NET 10 behavioral changes for affected APIs
3. Update test expectations if behavior change is intentional in .NET 10
4. Fix code if behavior change exposes a bug

**If performance degrades**:
1. Profile application with .NET 10 tooling
2. Review .NET 10 performance notes for known regressions
3. Report issue to .NET team if regression confirmed
4. Consider workarounds or wait for .NET 10 patch release

**Rollback Procedure**:
```bash
git checkout main
dotnet restore
dotnet build
```
All changes isolated on `upgrade-to-NET10` branch - rollback is instant.

## Testing & Validation Strategy

### Multi-Level Testing Approach

Testing follows the All-At-Once strategy: validate the entire solution in .NET 10 state after the atomic upgrade completes.

### Phase-by-Phase Testing Requirements

**Single Atomic Phase - All Projects Simultaneously**

After all projects updated, packages upgraded, and code fixed:

1. **Build Validation**
   - Entire solution must build with 0 errors
   - Entire solution must build with 0 warnings
   - No package dependency conflicts

2. **Test Execution**
   - Run all test projects
   - All tests must pass

3. **Functional Validation**
   - Verify core Identity operations work as expected

### Per-Project Testing

#### AspNetCore.Identity.CosmosDb.csproj (Core Library)

**Build Validation**:
```bash
dotnet build AspNetCore.Identity.CosmosDb/AspNetCore.Identity.CosmosDb.csproj --configuration Release
```

**Success Criteria**:
- ✅ Builds with 0 errors
- ✅ Builds with 0 warnings
- ✅ No package restore errors
- ✅ All TimeSpan ambiguities resolved

**Smoke Tests** (via dependent test projects):
- UserStore CRUD operations
- RoleStore CRUD operations
- Claims management
- Token generation and validation

#### AspNetCore.Identity.CosmosDbCompat.Tests.csproj

**Build Validation**:
```bash
dotnet build AspNetCore.Identity.CosmosDbCompat.Tests/AspNetCore.Identity.CosmosDbCompat.Tests.csproj
```

**Test Execution**:
```bash
dotnet test AspNetCore.Identity.CosmosDbCompat.Tests/AspNetCore.Identity.CosmosDbCompat.Tests.csproj --configuration Release --logger "console;verbosity=detailed"
```

**Success Criteria**:
- ✅ Project builds successfully
- ✅ All unit tests pass
- ✅ Test coverage maintained (no regression)
- ✅ Compatibility scenarios validated

**Key Test Categories**:
- Backward compatibility with existing Identity data
- Migration scenarios
- Integration with standard ASP.NET Core Identity

#### AspNetCore.Identity.CosmosDb.Tests.csproj

**Build Validation**:
```bash
dotnet build AspNetCore.Identity.CosmosDb.Tests/AspNetCore.Identity.CosmosDb.Tests.csproj
```

**Test Execution**:
```bash
dotnet test AspNetCore.Identity.CosmosDb.Tests/AspNetCore.Identity.CosmosDb.Tests.csproj --configuration Release --logger "console;verbosity=detailed"
```

**Success Criteria**:
- ✅ Project builds successfully
- ✅ All unit tests pass
- ✅ Test coverage maintained (no regression)
- ✅ V9-specific features validated

**Key Test Categories**:
- Latest feature tests
- Performance tests (if any)
- Cosmos DB-specific scenarios

### Comprehensive Validation (End-to-End)

**Solution-Wide Build**:
```bash
dotnet build AspNetCore.Identity.CosmosDb.sln --configuration Release
```

**Solution-Wide Test Execution**:
```bash
dotnet test AspNetCore.Identity.CosmosDb.sln --configuration Release --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

**Expected Results**:
- ✅ All projects build successfully
- ✅ All tests pass (100% pass rate)
- ✅ No test execution errors
- ✅ Code coverage >= baseline (if measured previously)

### Testing Checklist

**Pre-Upgrade Baseline** (recommended):
- [ ] Capture test results on .NET 9 (before upgrade)
- [ ] Capture test execution time baseline
- [ ] Document any known flaky tests
- [ ] Verify Cosmos DB emulator/test instance is accessible

**Post-Upgrade Validation**:
- [ ] All 3 projects build without errors
- [ ] All 3 projects build without warnings
- [ ] No NuGet package conflicts
- [ ] AspNetCore.Identity.CosmosDbCompat.Tests: All tests pass
- [ ] AspNetCore.Identity.CosmosDb.Tests: All tests pass
- [ ] Test execution time comparable to baseline (no major regression)
- [ ] Code coverage maintained or improved

**Integration Validation** (if applicable):
- [ ] Application starts successfully (if there's a sample app)
- [ ] User registration works
- [ ] User login works
- [ ] Claims retrieval works
- [ ] Role assignment works
- [ ] Cosmos DB connection stable

**Performance Validation** (optional but recommended):
- [ ] Query performance similar to .NET 9
- [ ] Memory usage comparable
- [ ] Startup time comparable

### Test Environment Requirements

**Prerequisites**:
- .NET 10 SDK (10.0.104 or higher)
- Cosmos DB Emulator installed and running, OR
- Access to Cosmos DB test instance (connection string in UserSecrets)

**Configuration**:
- Verify UserSecrets configured for both test projects
- Ensure Cosmos DB endpoints and keys are valid
- Verify test data can be created/cleaned up

**Clean Test Runs**:
```bash
# Clean solution before testing
dotnet clean AspNetCore.Identity.CosmosDb.sln
dotnet restore AspNetCore.Identity.CosmosDb.sln --force-evaluate
dotnet build AspNetCore.Identity.CosmosDb.sln
dotnet test AspNetCore.Identity.CosmosDb.sln
```

### Failure Investigation

**If Tests Fail**:

1. **Categorize Failures**:
   - Compilation errors → Review breaking changes catalog
   - Runtime errors → Check for behavioral changes in .NET 10
   - Assertion failures → Verify test expectations still valid
   - Infrastructure errors → Check Cosmos DB connection

2. **Isolate Root Cause**:
   ```bash
   # Run single failing test
   dotnet test --filter "FullyQualifiedName~TestNameHere"
   ```

3. **Common Issues**:
   - TimeSpan overloads not fixed → Add explicit casts
   - Cosmos DB connection → Verify emulator running or connection string valid
   - JSON serialization differences → Review System.Text.Json 10.0 changes
   - Identity behavior changes → Review ASP.NET Core 10.0 migration guide

4. **Rollback if Needed**:
   ```bash
   git checkout main
   dotnet restore
   dotnet build
   dotnet test
   ```

### Success Criteria Summary

**Upgrade is successful when**:
- ✅ Solution builds with 0 errors and 0 warnings
- ✅ All tests pass (100% pass rate)
- ✅ Test coverage maintained
- ✅ No performance regressions observed
- ✅ No package dependency conflicts
- ✅ No security vulnerabilities introduced

**Proceed to merge when all criteria met.**

## Complexity & Effort Assessment

### Overall Complexity: **Low**

The upgrade is straightforward due to the small solution size, simple dependency structure, and minimal breaking changes.

### Per-Project Complexity

| Project | Complexity | Dependencies | Packages to Update | API Issues | Risk | Justification |
|:---|:---:|:---:|:---:|:---:|:---:|:---|
| **AspNetCore.Identity.CosmosDb** | 🟢 Low | 0 | 6 | 3 | Low | Core library with 3 source-incompatible APIs (TimeSpan); straightforward fixes |
| **AspNetCore.Identity.CosmosDbCompat.Tests** | 🟢 Low | 1 | 1 | 1 | Low | Test project with single package update and 1 API issue |
| **AspNetCore.Identity.CosmosDb.Tests** | 🟢 Low | 1 | 3 | 1 | Low | Test project with 3 package updates and 1 API issue |

### Phase Complexity Assessment

**Single Atomic Phase - All Projects Simultaneously**:
- **Complexity**: 🟢 Low
- **Scope**: Update 3 project files, 7 package references, fix 5 API incompatibilities
- **Estimated Impact**: 5+ LOC across 8 files (0.1% of codebase)
- **Dependency Ordering**: Core library errors must be resolved before test projects can build

### Relative Complexity Ratings

**Low Complexity** (All 3 projects):
- Standard framework version update (net9.0 → net10.0)
- Package updates with known compatible versions
- Minor API incompatibilities (overload selection)
- No architectural changes required
- No configuration migrations needed

### Resource Requirements

**Skill Levels Needed**:
- **Core Library Update**: Mid-level .NET developer familiar with ASP.NET Core Identity and EF Core
- **Test Project Updates**: Junior to mid-level developer (simpler than core library)
- **API Fixes**: Mid-level developer (understanding of method overload resolution and type casting)

**Parallel Capacity**:
- **All-At-Once Strategy**: Single developer can execute entire upgrade
- **Optional Parallelization**: If desired, one developer can handle core library while another prepares for test project updates (though test projects cannot build until core builds)

**Prerequisites**:
- .NET 10 SDK installed (10.0.104 or later)
- Familiarity with solution structure
- Access to Cosmos DB emulator or test instance (for test execution)

### Effort Distribution

The upgrade effort is concentrated in a single atomic operation:

1. **Project File Updates** (Trivial): Change `<TargetFramework>` in 3 files
2. **Package Updates** (Low): Update 7 package references to .NET 10 versions
3. **Dependency Restore** (Automated): `dotnet restore`
4. **Compilation Error Fixes** (Low): Fix 5 source-incompatible API calls (TimeSpan overloads)
5. **Build Verification** (Automated): `dotnet build`
6. **Test Execution** (Automated): `dotnet test` on 2 test projects

**Note**: No real-time estimates provided. Complexity is relative (Low/Medium/High). Actual duration depends on developer familiarity, environment setup, and unforeseen issues.

## Source Control Strategy

### Branching Strategy

**Branch Structure**:
- **Main Branch**: `main` - Production-ready .NET 9.0 code
- **Upgrade Branch**: `upgrade-to-NET10` - All .NET 10 upgrade work (already created and active)
- **Source Branch**: `main` - Starting point for upgrade

**Current State**: Already on `upgrade-to-NET10` branch with clean working directory.

### Commit Strategy

**All-At-Once Single Commit Approach** (Recommended):

Since the upgrade is atomic, prefer a single comprehensive commit for the entire upgrade:

**Single Commit Structure**:
```bash
# After all changes complete and tests pass
git add -A
git commit -m "Upgrade solution to .NET 10.0

- Update all 3 projects to target net10.0
- Update 7 NuGet packages to .NET 10 versions
- Fix 5 TimeSpan overload ambiguities
- Verify all tests pass

Projects upgraded:
- AspNetCore.Identity.CosmosDb.csproj
- AspNetCore.Identity.CosmosDbCompat.Tests.csproj
- AspNetCore.Identity.CosmosDb.Tests.csproj

Package updates:
- Microsoft.AspNetCore.Identity.EntityFrameworkCore: 9.0.8 → 10.0.5
- Microsoft.AspNetCore.Identity.UI: 9.0.8 → 10.0.5
- Microsoft.EntityFrameworkCore.Cosmos: 9.0.8 → 10.0.5
- Microsoft.Extensions.Caching.Memory: 9.0.8 → 10.0.5
- Microsoft.Extensions.Configuration.UserSecrets: 9.0.8 → 10.0.5
- System.Text.Json: 9.0.8 → 10.0.5
- Newtonsoft.Json: 13.0.3 → 13.0.4

Tests: All tests passing (100% pass rate)"
```

**Alternative: Checkpoint Commits** (If preferred):

If you prefer incremental checkpoints:

1. **Commit 1: Framework Updates**
   ```bash
   git add *.csproj
   git commit -m "Update target framework to net10.0 in all projects"
   ```

2. **Commit 2: Package Updates**
   ```bash
   git add *.csproj
   git commit -m "Update NuGet packages to .NET 10 versions"
   ```

3. **Commit 3: Code Fixes**
   ```bash
   git add .
   git commit -m "Fix TimeSpan overload ambiguities for .NET 10 compatibility"
   ```

4. **Commit 4: Verification**
   ```bash
   git commit --allow-empty -m "Verify: All builds succeed, all tests pass"
   ```

**Recommended**: Single commit approach aligns with All-At-Once strategy and creates cleaner history.

### Review and Merge Process

**Pull Request Requirements**:

1. **Create Pull Request**:
   ```bash
   # Push upgrade branch
   git push origin upgrade-to-NET10

   # Create PR: upgrade-to-NET10 → main
   ```

2. **PR Description Template**:
   ```markdown
   ## .NET 10 Upgrade

   Upgrades entire solution from .NET 9.0 to .NET 10.0 (LTS).

   ### Changes
   - ✅ All 3 projects upgraded to net10.0
   - ✅ All 7 required packages updated
   - ✅ 5 TimeSpan API incompatibilities resolved
   - ✅ All tests passing

   ### Validation
   - [x] Solution builds with 0 errors
   - [x] Solution builds with 0 warnings
   - [x] All tests pass (AspNetCore.Identity.CosmosDbCompat.Tests)
   - [x] All tests pass (AspNetCore.Identity.CosmosDb.Tests)
   - [x] No package vulnerabilities
   - [x] No package conflicts

   ### Breaking Changes
   None - all source incompatibilities resolved at compile time.

   ### Rollback Plan
   Revert PR or `git checkout main` to return to .NET 9.0.
   ```

3. **Review Checklist**:
   - [ ] All project files updated to net10.0
   - [ ] All package versions correct (see Package Update Reference)
   - [ ] All TimeSpan fixes applied (no hardcoded int literals)
   - [ ] CI/CD pipeline passes (if configured)
   - [ ] Test results attached or linked
   - [ ] No unintended changes (e.g., formatting, whitespace)

4. **Merge Criteria**:
   - ✅ At least 1 reviewer approval (if team policy requires)
   - ✅ All CI checks pass
   - ✅ No merge conflicts
   - ✅ All tests passing
   - ✅ No package vulnerabilities

5. **Merge Method**:
   - **Recommended**: Squash and merge (if using checkpoint commits)
   - **Alternative**: Merge commit (if using single commit)
   - **Not Recommended**: Rebase (preserves commit history but less clean for feature branch)

### Post-Merge Actions

**After Merge to `main`**:

1. **Tag Release** (optional but recommended):
   ```bash
   git checkout main
   git pull origin main
   git tag -a v1.0.0-net10 -m "Release with .NET 10 support"
   git push origin v1.0.0-net10
   ```

2. **Update Documentation**:
   - Update README.md with .NET 10 requirement
   - Update CI/CD pipeline to use .NET 10 SDK
   - Update deployment documentation

3. **Clean Up Branch**:
   ```bash
   # Delete upgrade branch (after successful merge)
   git branch -d upgrade-to-NET10
   git push origin --delete upgrade-to-NET10
   ```

4. **Notify Team**:
   - Announce .NET 10 upgrade completion
   - Share migration notes if any issues encountered
   - Update developer setup documentation

### Rollback Procedure

**If Issues Arise After Merge**:

**Option 1: Revert PR**:
```bash
git checkout main
git pull origin main
git revert -m 1 <merge-commit-sha>
git push origin main
```

**Option 2: Emergency Rollback**:
```bash
git checkout main
git reset --hard <commit-before-merge>
git push origin main --force  # Use with caution
```

**Option 3: Fix Forward**:
- Create hotfix branch from main
- Apply targeted fixes
- Fast-track PR review and merge

### Git Best Practices

**Before Committing**:
- [ ] Run `dotnet format` (if using code formatting)
- [ ] Run `dotnet build` to verify no errors
- [ ] Run `dotnet test` to verify all tests pass
- [ ] Review `git diff` to ensure only intentional changes included

**Commit Message Guidelines**:
- Use imperative mood ("Update" not "Updated")
- Keep subject line under 72 characters
- Include detailed body for complex changes
- Reference issue numbers if applicable

**Branch Hygiene**:
- Keep upgrade branch focused (no unrelated changes)
- Rebase on main if main has advanced (before PR)
- Resolve conflicts carefully (preserve upgrade changes)

## Success Criteria

### Technical Criteria

The .NET 10 upgrade is technically successful when all of the following are met:

#### All Projects Migrated
- ✅ **AspNetCore.Identity.CosmosDb.csproj** targets net10.0
- ✅ **AspNetCore.Identity.CosmosDbCompat.Tests.csproj** targets net10.0
- ✅ **AspNetCore.Identity.CosmosDb.Tests.csproj** targets net10.0

#### All Packages Updated
- ✅ All 7 packages requiring updates are upgraded to .NET 10 versions:
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore: 10.0.5
  - Microsoft.AspNetCore.Identity.UI: 10.0.5
  - Microsoft.EntityFrameworkCore.Cosmos: 10.0.5
  - Microsoft.Extensions.Caching.Memory: 10.0.5
  - Microsoft.Extensions.Configuration.UserSecrets: 10.0.5
  - System.Text.Json: 10.0.5
  - Newtonsoft.Json: 13.0.4

#### All Builds Pass
- ✅ `dotnet build AspNetCore.Identity.CosmosDb.sln` succeeds with 0 errors
- ✅ `dotnet build AspNetCore.Identity.CosmosDb.sln` produces 0 warnings
- ✅ No package dependency conflicts
- ✅ No package restore errors

#### All Tests Pass
- ✅ `dotnet test AspNetCore.Identity.CosmosDbCompat.Tests.csproj` passes (100% pass rate)
- ✅ `dotnet test AspNetCore.Identity.CosmosDb.Tests.csproj` passes (100% pass rate)
- ✅ No test execution errors
- ✅ No test timeouts

#### No Vulnerabilities
- ✅ `dotnet list package --vulnerable` reports no vulnerabilities
- ✅ No deprecated packages in use

### Quality Criteria

The upgrade maintains or improves quality when:

#### Code Quality Maintained
- ✅ All 5 TimeSpan API incompatibilities resolved with explicit type casts
- ✅ No compiler warnings introduced
- ✅ No code smells introduced (e.g., suppressed warnings, quick hacks)
- ✅ Code style consistent with existing codebase

#### Test Coverage Maintained
- ✅ All existing tests pass (no tests removed to achieve passing status)
- ✅ Test coverage >= baseline (if measured previously)
- ✅ No tests marked as `[Ignore]` or skipped without justification

#### Documentation Updated
- ✅ README.md reflects .NET 10 requirement
- ✅ CI/CD configuration updated (if applicable)
- ✅ Developer setup guide updated
- ✅ This plan.md archived as reference

### Process Criteria

The upgrade follows the defined process when:

#### All-At-Once Strategy Followed
- ✅ All 3 projects upgraded simultaneously (no interim multi-targeting)
- ✅ All package updates applied atomically
- ✅ Solution moved directly from .NET 9 to .NET 10 (no intermediate states)

#### Source Control Strategy Followed
- ✅ All work performed on `upgrade-to-NET10` branch
- ✅ Single comprehensive commit OR logical checkpoint commits
- ✅ Pull request created with proper description
- ✅ Code review completed (if team policy requires)

#### Testing Strategy Followed
- ✅ Build validation performed before test execution
- ✅ All test projects executed
- ✅ Integration validation performed (if applicable)
- ✅ Test results documented

### All-At-Once Strategy-Specific Criteria

Per the All-At-Once strategy principles:

#### Atomic Operation Completed
- ✅ All project files updated in single operation
- ✅ All package references updated in single operation
- ✅ All compilation errors fixed before tests executed
- ✅ No projects left in .NET 9 state

#### No Intermediate States
- ✅ No multi-targeting used (e.g., `<TargetFrameworks>net9.0;net10.0</TargetFrameworks>`)
- ✅ No phased rollout (all projects .NET 10 simultaneously)
- ✅ No conditional compilation for framework differences

#### Clean Dependency Resolution
- ✅ All packages resolve against .NET 10 simultaneously
- ✅ No package version conflicts between .NET 9 and .NET 10
- ✅ Dependency graph remains clean (no circular dependencies introduced)

#### Unified Testing
- ✅ Entire solution tested in .NET 10 state at once
- ✅ No tests skipped due to framework differences
- ✅ Test suite validates complete .NET 10 solution

### Verification Commands

Run these commands to verify success criteria:

```bash
# Verify framework versions
grep -r "TargetFramework" --include="*.csproj" | grep -v "net10.0" && echo "❌ Not all projects on net10.0" || echo "✅ All projects on net10.0"

# Verify builds
dotnet build AspNetCore.Identity.CosmosDb.sln --no-incremental && echo "✅ Build successful" || echo "❌ Build failed"

# Verify no warnings
dotnet build AspNetCore.Identity.CosmosDb.sln --no-incremental 2>&1 | grep -i "warning" && echo "❌ Warnings present" || echo "✅ No warnings"

# Verify tests
dotnet test AspNetCore.Identity.CosmosDb.sln --no-build && echo "✅ All tests passed" || echo "❌ Tests failed"

# Verify vulnerabilities
dotnet list package --vulnerable && echo "Check output for vulnerabilities" || echo "✅ No vulnerabilities found"

# Verify package versions
dotnet list package | grep -E "(Microsoft\.AspNetCore\.Identity|Microsoft\.EntityFrameworkCore|Microsoft\.Extensions|System\.Text\.Json)" && echo "Verify versions are 10.0.5 or higher"
```

### Definition of Done

**The upgrade is DONE when**:

1. ✅ All technical criteria met (builds, tests, packages, vulnerabilities)
2. ✅ All quality criteria met (code quality, test coverage, documentation)
3. ✅ All process criteria met (strategy followed, source control, testing)
4. ✅ All verification commands pass
5. ✅ Pull request approved and merged to `main`
6. ✅ Post-merge validation confirms `main` branch works correctly

**At this point**:
- Solution fully operates on .NET 10.0
- All developers can pull `main` and build successfully
- CI/CD pipeline builds and tests successfully
- No .NET 9 artifacts remain in active codebase
- Upgrade branch can be safely deleted

---

## Identity Passkey Impact Addendum (.NET 10)

### Research Summary

Based on the provided sources (ASP.NET Core 10 breaking changes docs, passkey PR `dotnet/aspnetcore#62112`, and implementation write-ups), .NET 10 introduces **new ASP.NET Core Identity passkey capabilities** (WebAuthn) that can affect custom Identity stores.

### Why This Repository Is Affected

This repository provides a custom Identity store (`CosmosUserStore`) and custom model mapping (`CosmosIdentityDbContext` + `ModelBuilderExtensions`) for Cosmos DB.

Current codebase observations:
- `CosmosUserStore` implements many Identity store interfaces, but **does not implement passkey store abstractions**.
- No passkey entity/mapping exists (`IdentityUserPasskey` / `AspNetUserPasskeys` equivalent not found).
- No passkey-related repository surface exists.

### Expected Impact

1. **No immediate compile failure is guaranteed** just by upgrading packages/TFM.
2. **Passkey operations will likely be unsupported at runtime** with current store implementation.
3. If consumers adopt .NET 10 passkey APIs (`UserManager`/`SignInManager` passkey workflows), they will likely encounter "store does not implement required passkey store" behavior.

### Decision Required

- **Option A (Recommended for parity):** Implement passkey persistence support in this provider.
- **Option B (Explicitly unsupported):** Keep passkeys unsupported and document limitation clearly for .NET 10 users.

### Added Task (Requested)

- [ ] **TASK-PASSKEY-001: Implement ASP.NET Core Identity passkey support in Cosmos provider**
  - Implement passkey store support in `CosmosUserStore` (or companion store) using .NET 10 Identity passkey abstractions.
  - Add passkey persistence model + Cosmos mapping for credential data.
  - Add repository methods for passkey CRUD by `UserId` and `CredentialId`.
  - Add integration tests validating passkey registration and sign-in flows with `UserManager`/`SignInManager`.
  - Update `README.md` with passkey support details and limitations.

### Updated TODO List (Passkey Workstream)

#### P0 - Required for clear .NET 10 upgrade outcome
- [ ] **Decide support stance**: passkeys supported vs explicitly unsupported.
- [ ] **Document current behavior** in `README.md` for .NET 10 (passkey support status).
- [ ] **Add upgrade note** in release notes/changelog describing passkey implications.

#### P1 - If passkeys will be supported in this provider
- [ ] **Add passkey data model** compatible with ASP.NET Core Identity .NET 10 passkey expectations (credential id, user id, serialized passkey data/metadata).
- [ ] **Add EF Cosmos mapping** for passkey entity (container/key strategy aligned with current Cosmos mapping conventions).
- [ ] **Extend repository contracts** to query/save/delete passkey records efficiently by user and credential id.
- [ ] **Implement passkey store interface(s)** on `CosmosUserStore` (or companion store type), including add/update, lookup, list, and delete operations required by Identity passkey flows.
- [ ] **Validate serialization format stability** for stored passkey payloads across SDK patch versions.

#### P2 - Validation and quality
- [ ] **Add unit/integration tests** for passkey lifecycle: register, rename/metadata update (if applicable), sign-in assertion lookup, revoke/delete.
- [ ] **Add interop tests** against .NET 10 Identity APIs to ensure `UserManager`/`SignInManager` passkey methods function with Cosmos store.
- [ ] **Add negative tests** for malformed credential payloads and missing credential scenarios.
- [ ] **Add migration/back-compat tests** ensuring existing non-passkey users and data remain unaffected.

#### P3 - Optional but recommended
- [ ] **Expose capability flag** or helper indicating passkey support availability for consumers.
- [ ] **Add sample usage** (minimal passkey registration/auth flow) for Cosmos-backed Identity.
- [ ] **Add observability hooks** for passkey success/failure metrics aligned with .NET 10 Identity metrics.

### Secondary Breaking-Change Checklist (.NET 10 / ASP.NET Core 10)

Use this checklist during execution to ensure non-passkey .NET 10 changes are not missed.

| Item | Change Type | Applies to this repo now? | Verification / Action |
|:---|:---|:---:|:---|
| Cookie login redirects disabled for known API endpoints | Behavioral | ⚪ Indirect (library repo) | If adding/maintaining sample API hosts, verify endpoints return expected `401/403` instead of redirect loops. |
| `IActionContextAccessor` / `ActionContextAccessor` obsolete | Source | ⚪ Indirect | Search for usage in host/sample apps and replace with `IHttpContextAccessor` + endpoint metadata patterns. |
| `WebHostBuilder` / `IWebHost` / `WebHost` obsolete | Source | ⚪ Indirect | Ensure any sample host code uses Generic Host / `WebApplicationBuilder`. |
| `IPNetwork` and `ForwardedHeadersOptions.KnownNetworks` obsolete | Source | ⚪ Indirect | If forwarded headers configured in sample host, migrate to `KnownIPNetworks`. |
| Razor runtime compilation obsolete | Source | ⚪ Indirect | Remove `AddRazorRuntimeCompilation` from any sample apps; rely on Hot Reload in dev. |
| `WithOpenApi` deprecation / OpenAPI analyzer deprecations | Source | ⚪ Indirect | Validate OpenAPI generation in sample APIs and update deprecated APIs/properties. |
| Exception diagnostics suppression when `TryHandleAsync` returns `true` | Behavioral | ⚪ Indirect | If custom exception handlers exist in sample host, verify diagnostics/telemetry expectations. |
| Default .NET container base image switched to Ubuntu | Behavioral/Ops | ⚪ Indirect | If publishing container images, verify OS-package compatibility, startup scripts, and CI image assumptions. |
| `Uri` length limits removed | Behavioral | ⚪ Indirect | Add/request guardrails in host API layers if long URI inputs are security/perf concern. |
| X509/public key parameter APIs may return `null` | Behavioral/Source | ⚪ Potential | Audit certificate-processing code paths for nullability checks before ASN.1 operations. |

**Status legend**:
- ✅ Direct: present in this repository today
- ⚪ Indirect: mainly relevant to consuming/sample host applications
- ⚠️ Potential: may affect repo depending on usage patterns

**Prioritization for this repository**:
1. Passkey support gap (primary)
2. Certificate nullability audit (`X509Certificate`/`PublicKey` parameters)
3. Host-level ASP.NET Core behavior/obsolescence checks in any samples/consumers

### Passkey Implementation Checklist (EF Cosmos Compatible)

- [ ] Confirm support stance (`supported` vs `explicitly unsupported`) for .NET 10 passkeys.
- [ ] Add passkey persistence entity with Cosmos-safe shape (`CredentialId`, `UserId`, `Data` JSON, optional metadata).
- [ ] Configure EF mapping to Cosmos container (for example `AspNetUserPasskeys`) with explicit key and partition key.
- [ ] Ensure mapping avoids relational-only configuration (`HasIndex`, relational annotations, migrations assumptions).
- [ ] Extend repository contract for passkey CRUD:
  - [ ] Get by credential id
  - [ ] List by user id
  - [ ] Upsert
  - [ ] Delete single
  - [ ] Delete all user passkeys (optional)
- [ ] Implement required .NET 10 Identity passkey store interface(s) in `CosmosUserStore` (or companion store).
- [ ] Validate `UserManager`/`SignInManager` passkey flows with custom store.
- [ ] Add serialization/versioning rules for stored passkey `Data` JSON.
- [ ] Add tests:
  - [ ] Register passkey
  - [ ] Assertion lookup/sign-in
  - [ ] List/revoke/delete
  - [ ] Malformed payload handling
  - [ ] Backward compatibility for existing users
- [ ] Update `README.md` and release notes with passkey capability and limitations.

### EF Core 10 Cosmos Compatibility Checklist

- [ ] Keep `HasDiscriminatorInJsonIds()` behavior validated after upgrade.
- [ ] Verify no default Identity relational model configuration is reintroduced.
- [ ] Verify no `HasIndex` usage exists in Cosmos-mapped entities (including new passkey entity).
- [ ] Validate key and partition strategy for passkey container (`UserId` partition + `CredentialId` key or equivalent).
- [ ] Validate query translation for passkey lookup/list operations in EF 10 Cosmos.
- [ ] Validate upsert/update behavior for passkey writes under Cosmos provider.
- [ ] Validate delete semantics and cascade expectations for passkey records.
- [ ] Confirm no cross-partition hot paths for common auth operations.
- [ ] Re-run integration tests against real Cosmos Emulator/instance after package upgrades.
- [ ] Document any EF 10 Cosmos provider caveats discovered during execution.

## Implementation Timeline

### Phase 0: Preparation (Already Complete)

✅ **Verify SDK installation**: .NET 10 SDK available
✅ **Repository setup**: On `upgrade-to-NET10` branch with clean state
✅ **Assessment complete**: All issues identified

### Phase 1: Atomic Upgrade

**Operations** (performed as single coordinated batch):

1. Update all project files to net10.0
   - AspNetCore.Identity.CosmosDb.csproj
   - AspNetCore.Identity.CosmosDbCompat.Tests.csproj
   - AspNetCore.Identity.CosmosDb.Tests.csproj

2. Update all package references
   - 6 packages in AspNetCore.Identity.CosmosDb
   - 1 package in AspNetCore.Identity.CosmosDbCompat.Tests
   - 3 packages in AspNetCore.Identity.CosmosDb.Tests

3. Restore dependencies
   ```bash
   dotnet restore AspNetCore.Identity.CosmosDb.sln --force-evaluate
   ```

4. Build solution and fix all compilation errors
   - Fix 5 TimeSpan overload ambiguities
   - Address any additional compiler errors

5. Verify solution builds with 0 errors

**Deliverables**: Solution builds with 0 errors and 0 warnings

### Phase 2: Test Validation

**Operations**:

1. Execute all test projects
   ```bash
   dotnet test AspNetCore.Identity.CosmosDb.sln
   ```

2. Address test failures (if any)
   - Investigate root cause
   - Apply fixes
   - Re-run tests

3. Verify all tests pass

**Deliverables**: All tests pass (100% pass rate)

### Phase 3: Finalization

**Operations**:

1. Run verification commands (see Success Criteria)
2. Update documentation (README, CI/CD configs)
3. Commit changes
4. Create pull request
5. Code review
6. Merge to `main`
7. Tag release (optional)
8. Clean up upgrade branch

**Deliverables**: .NET 10 upgrade merged to `main`, production-ready

---

**End of Plan**
