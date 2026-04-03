using AspNetCore.Identity.CosmosDb.Passkeys;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AspNetCore.Identity.CosmosDb.Extensions
{
    /// <summary>
    /// Reusable ASP.NET Core Identity passkey UI integration for web projects.
    /// </summary>
    public static class PasskeyUiIntegrationExtensions
    {
        private const string EmbeddedClientScriptName = "AspNetCore.Identity.CosmosDb.Passkeys.identity-passkeys.js";
        private static string? _cachedClientScript;

        /// <summary>
        /// Registers reusable passkey UI integration services and options.
        /// </summary>
        /// <param name="services">Application services.</param>
        /// <param name="configure">Optional passkey UI integration options configuration.</param>
        /// <returns>The service collection.</returns>
        public static IServiceCollection AddCosmosPasskeyUiIntegration(
            this IServiceCollection services,
            Action<PasskeyUiIntegrationOptions>? configure = null)
        {
            services.AddOptions<PasskeyUiIntegrationOptions>();
            if (configure != null)
            {
                services.Configure(configure);
            }

            // Ensure a stable antiforgery header so JS clients can post passkey payloads.
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            return services;
        }

        /// <summary>
        /// Maps reusable passkey API endpoints and a packaged client script endpoint.
        /// </summary>
        /// <typeparam name="TUser">Identity user type.</typeparam>
        /// <param name="endpoints">Endpoint route builder.</param>
        /// <returns>The endpoint convention builder for the passkey route group.</returns>
        public static IEndpointConventionBuilder MapCosmosPasskeyUiEndpoints<TUser>(
            this IEndpointRouteBuilder endpoints)
            where TUser : class
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<PasskeyUiIntegrationOptions>>().Value;
            var routePrefix = options.RoutePrefix.TrimEnd('/');
            var group = endpoints.MapGroup(routePrefix);

            group.MapGet("/client.js", () => Results.Content(GetClientScript(), "text/javascript"))
                .AllowAnonymous();

            group.MapGet("/list", async (
                HttpContext context,
                UserManager<TUser> userManager,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("CosmosPasskeyUi");
                try
                {
                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                    {
                        return Results.Unauthorized();
                    }

                    var passkeys = await userManager.GetPasskeysAsync(user);
                    var response = passkeys.Select(pk => new
                    {
                        id = Convert.ToBase64String(pk.CredentialId),
                        name = pk.Name,
                        createdAt = pk.CreatedAt,
                        signCount = pk.SignCount,
                        isUserVerified = pk.IsUserVerified,
                        isBackupEligible = pk.IsBackupEligible,
                        isBackedUp = pk.IsBackedUp
                    }).ToList();

                    return Results.Ok(response);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error listing passkeys.");
                    return Results.Problem("Failed to list passkeys.", statusCode: StatusCodes.Status500InternalServerError);
                }
            }).RequireAuthorization();

            group.MapPost("/creation-options", async (
                HttpContext context,
                UserManager<TUser> userManager,
                SignInManager<TUser> signInManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                ILoggerFactory loggerFactory) =>
            {
                var logger = loggerFactory.CreateLogger("CosmosPasskeyUi");
                try
                {
                    await ValidateAntiforgeryAsync(options, context, antiforgery);

                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                    {
                        return Results.Unauthorized();
                    }

                    var userId = await userManager.GetUserIdAsync(user);
                    var userName = await userManager.GetUserNameAsync(user) ?? "User";
                    var optionsJson = await signInManager.MakePasskeyCreationOptionsAsync(new()
                    {
                        Id = userId,
                        Name = userName,
                        DisplayName = userName
                    });

                    return Results.Content(optionsJson, "application/json");
                }
                catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                {
                    return Results.BadRequest(new { error = "Invalid antiforgery token." });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating passkey creation options.");
                    return Results.Problem("Failed to create passkey options.", statusCode: StatusCodes.Status500InternalServerError);
                }
            }).RequireAuthorization();

            group.MapPost("/request-options", async (
                HttpContext context,
                UserManager<TUser> userManager,
                SignInManager<TUser> signInManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                ILoggerFactory loggerFactory,
                string? username) =>
            {
                var logger = loggerFactory.CreateLogger("CosmosPasskeyUi");
                try
                {
                    await ValidateAntiforgeryAsync(options, context, antiforgery);

                    var user = string.IsNullOrWhiteSpace(username)
                        ? null
                        : await userManager.FindByNameAsync(username);

                    var optionsJson = await signInManager.MakePasskeyRequestOptionsAsync(user);
                    return Results.Content(optionsJson, "application/json");
                }
                catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                {
                    return Results.BadRequest(new { error = "Invalid antiforgery token." });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error creating passkey request options.");
                    return Results.Problem("Failed to create passkey request options.", statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AllowAnonymous();

            group.MapPost("/register", async (
                HttpContext context,
                UserManager<TUser> userManager,
                SignInManager<TUser> signInManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                ILoggerFactory loggerFactory,
                RegisterPasskeyRequest request) =>
            {
                var logger = loggerFactory.CreateLogger("CosmosPasskeyUi");
                try
                {
                    await ValidateAntiforgeryAsync(options, context, antiforgery);

                    if (string.IsNullOrWhiteSpace(request.CredentialJson))
                    {
                        return Results.BadRequest(new { error = "Credential JSON is required." });
                    }

                    var user = await userManager.GetUserAsync(context.User);
                    if (user is null)
                    {
                        return Results.Unauthorized();
                    }

                    var passkeys = await userManager.GetPasskeysAsync(user);
                    if (passkeys.Count >= options.MaxPasskeysPerUser)
                    {
                        return Results.BadRequest(new { error = "Maximum passkey count reached." });
                    }

                    var attestationResult = await signInManager.PerformPasskeyAttestationAsync(request.CredentialJson);
                    if (!attestationResult.Succeeded)
                    {
                        return Results.BadRequest(new { error = attestationResult.Failure.Message });
                    }

                    if (!string.IsNullOrWhiteSpace(request.Name))
                    {
                        var trimmedName = request.Name.Trim();
                        if (trimmedName.Length > options.MaxPasskeyNameLength)
                        {
                            return Results.BadRequest(new
                            {
                                error = $"Passkey names must be no longer than {options.MaxPasskeyNameLength} characters."
                            });
                        }

                        attestationResult.Passkey.Name = trimmedName;
                    }
                    else
                    {
                        attestationResult.Passkey.Name = $"Passkey {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC";
                    }

                    var addResult = await userManager.AddOrUpdatePasskeyAsync(user, attestationResult.Passkey);
                    if (!addResult.Succeeded)
                    {
                        return Results.BadRequest(new
                        {
                            error = "Failed to store passkey.",
                            details = addResult.Errors.Select(e => e.Description)
                        });
                    }

                    return Results.Ok(new
                    {
                        success = true,
                        id = Convert.ToBase64String(attestationResult.Passkey.CredentialId),
                        name = attestationResult.Passkey.Name
                    });
                }
                catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                {
                    return Results.BadRequest(new { error = "Invalid antiforgery token." });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error registering passkey.");
                    return Results.Problem("Failed to register passkey.", statusCode: StatusCodes.Status500InternalServerError);
                }
            }).RequireAuthorization();

            group.MapPost("/authenticate", async (
                HttpContext context,
                SignInManager<TUser> signInManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                AuthenticatePasskeyRequest request) =>
            {
                try
                {
                    await ValidateAntiforgeryAsync(options, context, antiforgery);

                    if (string.IsNullOrWhiteSpace(request.CredentialJson))
                    {
                        return Results.BadRequest(new { error = "Credential JSON is required." });
                    }

                    var result = await signInManager.PasskeySignInAsync(request.CredentialJson);
                    return result.Succeeded
                        ? Results.Ok(new { success = true })
                        : Results.Unauthorized();
                }
                catch (Microsoft.AspNetCore.Antiforgery.AntiforgeryValidationException)
                {
                    return Results.BadRequest(new { error = "Invalid antiforgery token." });
                }
            }).AllowAnonymous();

            group.MapPost("/remove", async (
                HttpContext context,
                UserManager<TUser> userManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                RemovePasskeyRequest request) =>
            {
                await ValidateAntiforgeryAsync(options, context, antiforgery);

                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    return Results.BadRequest(new { error = "Passkey ID is required." });
                }

                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                byte[] credentialId;
                try
                {
                    credentialId = Convert.FromBase64String(request.Id);
                }
                catch (FormatException)
                {
                    return Results.BadRequest(new { error = "Passkey ID has an invalid format." });
                }

                var result = await userManager.RemovePasskeyAsync(user, credentialId);
                return result.Succeeded
                    ? Results.Ok(new { success = true })
                    : Results.BadRequest(new
                    {
                        error = "Failed to remove passkey.",
                        details = result.Errors.Select(e => e.Description)
                    });
            }).RequireAuthorization();

            group.MapPost("/rename", async (
                HttpContext context,
                UserManager<TUser> userManager,
                Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery,
                RenamePasskeyRequest request) =>
            {
                await ValidateAntiforgeryAsync(options, context, antiforgery);

                if (string.IsNullOrWhiteSpace(request.Id))
                {
                    return Results.BadRequest(new { error = "Passkey ID is required." });
                }

                var name = request.Name?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Results.BadRequest(new { error = "Passkey name is required." });
                }

                if (name.Length > options.MaxPasskeyNameLength)
                {
                    return Results.BadRequest(new
                    {
                        error = $"Passkey names must be no longer than {options.MaxPasskeyNameLength} characters."
                    });
                }

                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.Unauthorized();
                }

                byte[] credentialId;
                try
                {
                    credentialId = Convert.FromBase64String(request.Id);
                }
                catch (FormatException)
                {
                    return Results.BadRequest(new { error = "Passkey ID has an invalid format." });
                }

                var passkey = await userManager.GetPasskeyAsync(user, credentialId);
                if (passkey is null)
                {
                    return Results.NotFound(new { error = "Passkey not found." });
                }

                passkey.Name = name;
                var result = await userManager.AddOrUpdatePasskeyAsync(user, passkey);
                return result.Succeeded
                    ? Results.Ok(new { success = true })
                    : Results.BadRequest(new
                    {
                        error = "Failed to rename passkey.",
                        details = result.Errors.Select(e => e.Description)
                    });
            }).RequireAuthorization();

            return group;
        }

        private static async Task ValidateAntiforgeryAsync(
            PasskeyUiIntegrationOptions options,
            HttpContext context,
            Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
        {
            if (!options.RequireAntiforgery)
            {
                return;
            }

            await antiforgery.ValidateRequestAsync(context);
        }

        private static string GetClientScript()
        {
            if (!string.IsNullOrEmpty(_cachedClientScript))
            {
                return _cachedClientScript;
            }

            var assembly = typeof(PasskeyUiIntegrationExtensions).Assembly;
            using var stream = assembly.GetManifestResourceStream(EmbeddedClientScriptName)
                ?? throw new InvalidOperationException($"Embedded passkey client script '{EmbeddedClientScriptName}' was not found.");
            using var reader = new StreamReader(stream);
            _cachedClientScript = reader.ReadToEnd();
            return _cachedClientScript;
        }
    }
}
