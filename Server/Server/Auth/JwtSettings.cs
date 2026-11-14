namespace Server.Auth
{
    /// <summary>Bound to the "Jwt" section of the configuration.</summary>
    public class JwtSettings
    {
        public const int MinKeyBytes = 32;

        public string Issuer { get; set; } = "GownRental";

        public string Audience { get; set; } = "GownRental.Client";

        /// <summary>Secret used to sign tokens. Never commit a real one: supply it through user-secrets or an environment variable.</summary>
        public string Key { get; set; } = string.Empty;

        public int ExpiryMinutes { get; set; } = 480;
    }
}
