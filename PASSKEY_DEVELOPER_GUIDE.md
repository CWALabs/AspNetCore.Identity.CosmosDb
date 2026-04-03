# Passkey Developer Guide

This guide provides comprehensive instructions for developers integrating passkey support into their applications using `AspNetCore.Identity.CosmosDb`.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Core Operations](#core-operations)
- [Integration Patterns](#integration-patterns)
- [Best Practices](#best-practices)
- [Performance Considerations](#performance-considerations)
- [Troubleshooting](#troubleshooting)
- [FAQ](#faq)

---

## Overview

Passkeys are a phishing-resistant authentication mechanism that combine public key cryptography with user-friendly experiences. The `IUserPasskeyStore<TUserEntity>` interface provides CRUD operations for managing passkeys in Cosmos DB.

### What You Can Do

- Register and store user passkeys
- Authenticate users with passkeys
- Manage multiple passkeys per user
- Update passkey metadata
- Remove compromised passkeys

### Key Concepts

- **Credential ID**: Unique identifier for a passkey (byte array)
- **Public Key**: The public key component of the passkey
- **Sign Count**: Counter preventing credential cloning
- **Transports**: Communication methods (e.g., "internal", "usb", "nfc")
- **User Verification**: Whether the passkey requires biometric/PIN verification

---

## Prerequisites

1. **.NET 10** runtime or later
2. **AspNetCore.Identity.CosmosDb** NuGet package
3. **Cosmos DB** configured with Identity tables
4. **WebAuthn library** (e.g., `Fido2.AspNet`) for client-side registration/authentication

### Project Setup

```xml
<PackageReference Include="AspNetCore.Identity.CosmosDb" Version="*" />
<PackageReference Include="Fido2.AspNet" Version="*" />
<PackageReference Include="Microsoft.AspNetCore.Identity" Version="10.0.0" />
```

---

## Getting Started

### 1. Configure Dependency Injection

```csharp
services.AddScoped<IUserPasskeyStore<IdentityUser>, CosmosUserStore<IdentityUser, IdentityRole, string>>();
services.AddScoped<UserManager<IdentityUser>>();
services.AddScoped<SignInManager<IdentityUser>>();
```

### 2. Inject UserManager in Your Controller

```csharp
public class PasskeysController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    
    public PasskeysController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }
}
```

---

## Core Operations

### Adding a Passkey

When a user registers a passkey, collect the credential data from your WebAuthn library and store it:

```csharp
public async Task<IActionResult> RegisterPasskey(string userName, UserPasskeyInfo passkey)
{
    var user = await _userManager.FindByNameAsync(userName);
    if (user == null)
        return NotFound("User not found");

    try
    {
        var passkeyStore = _userManager.UserValidators
            .OfType<IUserPasskeyStore<IdentityUser>>()
            .FirstOrDefault() as IUserPasskeyStore<IdentityUser>;

        await passkeyStore.AddOrUpdatePasskeyAsync(
            user,
            passkey,
            CancellationToken.None);

        return Ok("Passkey registered successfully");
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest($"Invalid input: {ex.Message}");
    }
}
```

**Expected `UserPasskeyInfo` Structure:**

```csharp
var passkeyInfo = new UserPasskeyInfo(
    credentialId: credentialIdBytes,           // Unique credential identifier
    publicKey: publicKeyBytes,                 // Public key component
    createdAt: DateTimeOffset.UtcNow,          // Registration timestamp
    signCount: 0,                              // Initial sign count
    transports: new[] { "internal", "usb" },   // Supported transports
    isUserVerified: true,                      // User verification flag
    isBackupEligible: true,                    // Can be backed up to cloud
    isBackedUp: false,                         // Currently backed up
    attestationObject: attestationObjBytes,    // Attestation data
    clientDataJson: clientDataBytes)           // Client data
{
    Name = "My Security Key"  // User-friendly passkey name
};
```

### Getting All Passkeys for a User

```csharp
public async Task<IActionResult> GetUserPasskeys(string userName)
{
    var user = await _userManager.FindByNameAsync(userName);
    if (user == null)
        return NotFound("User not found");

    try
    {
        var passkeyStore = _userManager.UserValidators
            .OfType<IUserPasskeyStore<IdentityUser>>()
            .FirstOrDefault() as IUserPasskeyStore<IdentityUser>;

        var passkeys = await passkeyStore.GetPasskeysAsync(user, CancellationToken.None);

        var result = passkeys.Select(p => new
        {
            p.Name,
            p.CreatedAt,
            p.SignCount,
            p.IsUserVerified,
            p.IsBackupEligible,
            p.IsBackedUp,
            CredentialIdBase64 = Convert.ToBase64String(p.CredentialId)
        }).ToList();

        return Ok(result);
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest($"Invalid input: {ex.Message}");
    }
}
```

### Finding a User by Passkey ID

Use this during authentication to identify which user owns a passkey:

```csharp
public async Task<IActionResult> AuthenticateWithPasskey(byte[] credentialId, string signature)
{
    try
    {
        var passkeyStore = _userManager.UserValidators
            .OfType<IUserPasskeyStore<IdentityUser>>()
            .FirstOrDefault() as IUserPasskeyStore<IdentityUser>;

        var user = await passkeyStore.FindByPasskeyIdAsync(credentialId, CancellationToken.None);
        if (user == null)
            return Unauthorized("Passkey not found");

        // Verify signature and continue authentication flow
        return Ok(new { UserId = user.Id, UserName = user.UserName });
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest($"Invalid credential ID: {ex.Message}");
    }
}
```

### Finding a Specific Passkey for a User

```csharp
public async Task<IActionResult> GetPasskeyDetails(string userName, string credentialIdBase64)
{
    var user = await _userManager.FindByNameAsync(userName);
    if (user == null)
        return NotFound("User not found");

    var credentialId = Convert.FromBase64String(credentialIdBase64);

    try
    {
        var passkeyStore = _userManager.UserValidators
            .OfType<IUserPasskeyStore<IdentityUser>>()
            .FirstOrDefault() as IUserPasskeyStore<IdentityUser>;

        var passkey = await passkeyStore.FindPasskeyAsync(user, credentialId, CancellationToken.None);
        if (passkey == null)
            return NotFound("Passkey not found");

        return Ok(new
        {
            passkey.Name,
            passkey.CreatedAt,
            passkey.SignCount,
            passkey.IsUserVerified,
            passkey.IsBackupEligible,
            passkey.IsBackedUp
        });
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest($"Invalid input: {ex.Message}");
    }
}
```

### Removing a Passkey

```csharp
public async Task<IActionResult> RemovePasskey(string userName, string credentialIdBase64)
{
    var user = await _userManager.FindByNameAsync(userName);
    if (user == null)
        return NotFound("User not found");

    var credentialId = Convert.FromBase64String(credentialIdBase64);

    try
    {
        var passkeyStore = _userManager.UserValidators
            .OfType<IUserPasskeyStore<IdentityUser>>()
            .FirstOrDefault() as IUserPasskeyStore<IdentityUser>;

        await passkeyStore.RemovePasskeyAsync(user, credentialId, CancellationToken.None);

        return Ok("Passkey removed successfully");
    }
    catch (ArgumentNullException ex)
    {
        return BadRequest($"Invalid input: {ex.Message}");
    }
}
```

---

## Integration Patterns

### Pattern 1: Two-Step Authentication (Email + Passkey)

```csharp
public async Task<IActionResult> LoginWithPasskey(string userName, byte[] credentialId)
{
    // Step 1: Find user by passkey
    var passkeyStore = /* get store */;
    var user = await passkeyStore.FindByPasskeyIdAsync(credentialId, CancellationToken.None);
    
    if (user == null)
        return Unauthorized("Passkey not recognized");

    // Step 2: Send confirmation email
    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
    // Send email with code...

    return Ok("Check your email to confirm passkey login");
}

public async Task<IActionResult> ConfirmPasskeyLogin(string userId, string emailCode)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user == null)
        return NotFound("User not found");

    var result = await _userManager.ConfirmEmailAsync(user, emailCode);
    if (!result.Succeeded)
        return BadRequest("Invalid confirmation code");

    // Issue authentication token
    return Ok(new { AuthToken = GenerateJwt(user) });
}
```

### Pattern 2: Backup Codes When Registering Passkey

```csharp
public async Task<IActionResult> RegisterPasskeyWithBackupCodes(
    string userName, 
    UserPasskeyInfo passkey)
{
    var user = await _userManager.FindByNameAsync(userName);
    if (user == null)
        return NotFound("User not found");

    // Add passkey
    var passkeyStore = /* get store */;
    await passkeyStore.AddOrUpdatePasskeyAsync(user, passkey, CancellationToken.None);

    // Generate backup codes
    var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);

    return Ok(new
    {
        Message = "Passkey registered successfully",
        BackupCodes = recoveryCodes,
        Note = "Save these codes in a secure location"
    });
}
```

### Pattern 3: Progressive Registration (Optional Passkey)

```csharp
public async Task<IActionResult> CompleteSignUp(SignUpModel model)
{
    // Create user with password
    var user = new IdentityUser { UserName = model.Email, Email = model.Email };
    var result = await _userManager.CreateAsync(user, model.Password);
    
    if (!result.Succeeded)
        return BadRequest(result.Errors);

    return Ok(new
    {
        Message = "Account created. Passkey registration is optional.",
        UserId = user.Id,
        NextStep = "/identity/passkeys/register"
    });
}
```

---

## Best Practices

### 1. **Validate Credential ID Length**

Credential IDs can vary in length. Validate before processing:

```csharp
if (credentialId == null || credentialId.Length == 0)
    throw new ArgumentException("Credential ID cannot be empty");

if (credentialId.Length > 1024)  // Adjust based on your requirements
    throw new ArgumentException("Credential ID is too large");
```

### 2. **Store Passkey Names for User Recognition**

Users need to identify their passkeys:

```csharp
var passkey = new UserPasskeyInfo(/* ... */)
{
    Name = model.PasskeyName ?? $"Passkey {DateTime.UtcNow:yyyy-MM-dd}"
};
```

### 3. **Monitor Sign Count for Cloning Detection**

Update sign count after each successful authentication:

```csharp
var passkey = await passkeyStore.FindPasskeyAsync(user, credentialId, ct);
if (passkey.SignCount > incomingSignCount)
{
    // Potential cloning attack detected
    await passkeyStore.RemovePasskeyAsync(user, credentialId, ct);
    return Unauthorized("Potential credential cloning detected");
}

// Update passkey with new sign count
var updated = new UserPasskeyInfo(
    credentialId,
    passkey.PublicKey,
    passkey.CreatedAt,
    incomingSignCount,  // Updated sign count
    passkey.Transports,
    passkey.IsUserVerified,
    passkey.IsBackupEligible,
    passkey.IsBackedUp,
    passkey.AttestationObject,
    passkey.ClientDataJson)
{
    Name = passkey.Name
};

await passkeyStore.AddOrUpdatePasskeyAsync(user, updated, ct);
```

### 4. **Require User Verification for Security-Critical Operations**

```csharp
if (!passkey.IsUserVerified)
{
    return BadRequest("This operation requires user verification. Please use a passkey with biometric or PIN verification.");
}
```

### 5. **Handle Null Credential ID Gracefully**

```csharp
if (passkey.CredentialId == null)
{
    _logger.LogWarning($"Passkey for user {user.Id} has null credential ID");
    return null;
}
```

---

## Performance Considerations

### 1. **Lookup by Credential ID (FindByPasskeyIdAsync)**

- **Current Implementation:** Scans all passkeys in memory
- **Recommendation:** For applications with < 10,000 passkeys, this is acceptable
- **For Large Scale:** Consider implementing a separate indexed lookup table

```csharp
// Example: Indexed lookup pattern
public async Task<TUserEntity> FindByPasskeyIdOptimizedAsync(byte[] credentialId)
{
    // Try indexed lookup first
    var indexed = await _repo.Table<PasskeyIndex>()
        .FirstOrDefaultAsync(p => p.CredentialIdHash == ComputeHash(credentialId));
    
    if (indexed != null)
        return await FindByIdAsync(indexed.UserId.ToString());
    
    // Fallback to full scan if not indexed
    return await FindByPasskeyIdAsync(credentialId, CancellationToken.None);
}
```

### 2. **Batch Operations**

```csharp
public async Task<IActionResult> RemoveMultiplePasskeys(string userName, List<string> credentialIdsBase64)
{
    var user = await _userManager.FindByNameAsync(userName);
    var passkeyStore = /* get store */;
    
    var tasks = credentialIdsBase64.Select(async credIdB64 =>
    {
        var credentialId = Convert.FromBase64String(credIdB64);
        await passkeyStore.RemovePasskeyAsync(user, credentialId, CancellationToken.None);
    });
    
    await Task.WhenAll(tasks);
    return Ok("Passkeys removed");
}
```

### 3. **Caching User's Passkey List**

```csharp
public async Task<IList<UserPasskeyInfo>> GetPasskeysCachedAsync(
    IdentityUser user,
    IMemoryCache cache,
    IUserPasskeyStore<IdentityUser> passkeyStore)
{
    var cacheKey = $"passkeys_{user.Id}";
    
    if (cache.TryGetValue(cacheKey, out IList<UserPasskeyInfo> passkeys))
        return passkeys;
    
    passkeys = await passkeyStore.GetPasskeysAsync(user, CancellationToken.None);
    
    // Cache for 5 minutes
    cache.Set(cacheKey, passkeys, TimeSpan.FromMinutes(5));
    
    return passkeys;
}
```

---

## Troubleshooting

### Issue: "ArgumentNullException: user cannot be null"

**Cause:** User was not found or is null

**Solution:**
```csharp
var user = await _userManager.FindByNameAsync(userName);
if (user == null)
    return NotFound("User does not exist");
```

### Issue: "Passkey not found" when calling FindPasskeyAsync

**Cause:** Credential ID mismatch or passkey was deleted

**Solution:**
```csharp
// Verify credential ID format
if (credentialId == null || credentialId.Length == 0)
    return BadRequest("Invalid credential ID");

// Check all user's passkeys
var allPasskeys = await passkeyStore.GetPasskeysAsync(user, ct);
if (!allPasskeys.Any())
    return BadRequest("User has no registered passkeys");
```

### Issue: Performance degradation when finding user by passkey ID

**Cause:** Large number of passkeys in database

**Solution:**
- Implement the indexed lookup pattern (see Performance Considerations)
- Monitor query latency with Application Insights
- Consider implementing rate limiting to prevent abuse

### Issue: Credential ID changes between requests

**Cause:** Encoding/decoding mismatch (Base64, Hex, etc.)

**Solution:**
```csharp
// Standardize encoding
string credentialIdHex = Convert.ToHexString(credentialIdBytes);
byte[] credentialIdBytes = Convert.FromHexString(credentialIdHex);
```

---

## FAQ

**Q: Can a user have multiple passkeys?**

A: Yes. Each passkey is identified by its unique credential ID, so users can register multiple passkeys on different devices.

**Q: What happens if I lose my passkey device?**

A: Users should have backup authentication methods (password, backup codes, recovery email). If needed, administrators can remove the lost passkey.

**Q: Are passkeys encrypted in Cosmos DB?**

A: Cosmos DB stores data encrypted at rest. Additionally, passkey data is stored in `IdentityPasskeyData` which includes encrypted components via EF Core.

**Q: What's the maximum number of passkeys per user?**

A: There's no hard limit in the code, but practical considerations:
- Each passkey adds ~500 bytes to storage
- Lookup performance degrades with very large numbers (1000+)
- Recommend limiting to 5-10 per user for UX

**Q: Can I migrate passkeys from another system?**

A: Yes, if you have the credential data:
```csharp
var importedPasskey = new UserPasskeyInfo(
    credentialId: /* from source system */,
    publicKey: /* from source system */,
    createdAt: /* from source system */,
    // ... other fields
);

await passkeyStore.AddOrUpdatePasskeyAsync(user, importedPasskey, ct);
```

**Q: How do I implement passwordless authentication?**

A: Combine passkey authentication with a second factor:
1. User enters email
2. System finds user by email
3. User authenticates with passkey
4. System issues authentication token

---

## Next Steps

1. Review the `UserPasskeyInfo` class definition in the source code
2. Examine `CosmosUserStore<TUserEntity, TRoleEntity, TKey>` implementation
3. Consult WebAuthn standards for client-side registration/authentication
4. Implement security measures for production (rate limiting, audit logging)

## Additional Resources

- [FIDO2 Specifications](https://fidoalliance.org/fido2/)
- [WebAuthn Standard](https://www.w3.org/TR/webauthn-2/)
- [ASP.NET Core Identity Documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [Cosmos DB Security](https://learn.microsoft.com/en-us/azure/cosmos-db/database-security)
