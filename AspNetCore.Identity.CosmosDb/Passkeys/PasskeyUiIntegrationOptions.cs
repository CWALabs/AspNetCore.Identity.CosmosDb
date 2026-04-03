namespace AspNetCore.Identity.CosmosDb.Passkeys
{
    /// <summary>
    /// Options for reusable passkey UI integration endpoints and client behavior.
    /// </summary>
    public sealed class PasskeyUiIntegrationOptions
    {
        /// <summary>
        /// API route prefix for passkey endpoints.
        /// </summary>
        public string RoutePrefix { get; set; } = "/identity/passkeys";

        /// <summary>
        /// Relative URL of the packaged client script endpoint.
        /// </summary>
        public string ClientScriptPath { get; set; } = "/identity/passkeys/client.js";

        /// <summary>
        /// Whether to enforce antiforgery validation for POST endpoints.
        /// </summary>
        public bool RequireAntiforgery { get; set; } = true;

        /// <summary>
        /// Maximum number of passkeys allowed per user.
        /// </summary>
        public int MaxPasskeysPerUser { get; set; } = 100;

        /// <summary>
        /// Maximum passkey display name length.
        /// </summary>
        public int MaxPasskeyNameLength { get; set; } = 200;
    }
}
