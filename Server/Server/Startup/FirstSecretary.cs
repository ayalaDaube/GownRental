using Bl.Api;
using Bl.Models;

namespace Server.Startup
{
    /// <summary>
    /// Creates the shop's first secretary account on the first start of a new installation, from configuration
    /// (<c>Bootstrap__Username</c> and <c>Bootstrap__Password</c> environment variables), so no password has to be
    /// written in a file. Once any account exists the settings are ignored, so a restart never resets a password.
    /// </summary>
    public static class FirstSecretary
    {
        public const int MinPasswordLength = 12;

        /// <summary>Returns true when the account was created. Settings that are half given or too weak stop the start with a clear message.</summary>
        public static bool CreateIfNeeded(IBLAuth auth, IConfiguration configuration)
        {
            var username = configuration["Bootstrap:Username"];
            var password = configuration["Bootstrap:Password"];
            var hasUsername = !string.IsNullOrWhiteSpace(username);
            var hasPassword = !string.IsNullOrEmpty(password);

            if (!hasUsername && !hasPassword)
            {
                return false;
            }

            if (hasUsername != hasPassword)
            {
                throw new InvalidOperationException("Bootstrap:Username and Bootstrap:Password must be set together.");
            }

            if (password!.Length < MinPasswordLength)
            {
                throw new InvalidOperationException($"Bootstrap:Password must be at least {MinPasswordLength} characters.");
            }

            return auth.EnsureFirstUser(username!, password, Roles.Secretary);
        }
    }
}
