using System.Text.Json.Serialization;

namespace AspNetCore.Identity.CosmosDb.Passkeys
{
    public sealed class RegisterPasskeyRequest
    {
        [JsonPropertyName("credentialJson")]
        public string? CredentialJson { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public sealed class AuthenticatePasskeyRequest
    {
        [JsonPropertyName("credentialJson")]
        public string? CredentialJson { get; set; }
    }

    public sealed class RemovePasskeyRequest
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    public sealed class RenamePasskeyRequest
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
