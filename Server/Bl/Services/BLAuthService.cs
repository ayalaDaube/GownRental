using Bl.Api;
using Bl.Models;
using Dal.Api;
using Dal.Models;
using Microsoft.AspNetCore.Identity;

namespace Bl.Services
{
    public class BLAuthService : IBLAuth
    {
        private const int MinPasswordLength = 8;

        private readonly IUser users;
        private readonly IPasswordHasher<AppUser> hasher = new PasswordHasher<AppUser>();

        // Verified against when the username does not exist, so that "unknown user" and
        // "wrong password" take about the same time and cannot be told apart by timing.
        private static readonly AppUser DummyUser = new() { Username = "-" };
        private static readonly string DummyHash =
            new PasswordHasher<AppUser>().HashPassword(DummyUser, Guid.NewGuid().ToString("N"));

        /// <summary>Usernames are not case sensitive: they are stored and looked up in lower case, so the rule does not depend on the database.</summary>
        private static string NormalizeUsername(string username) => username.Trim().ToLowerInvariant();

        public BLAuthService(IDal dal)
        {
            users = dal.User;
        }

        public AuthenticatedUser? ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return null;
            }

            var user = users.GetByUsername(NormalizeUsername(username));
            if (user == null)
            {
                hasher.VerifyHashedPassword(DummyUser, DummyHash, password);
                return null;
            }

            var result = Verify(user, password);
            if (result == PasswordVerificationResult.Failed)
            {
                return null;
            }

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                // The hashing algorithm was upgraded since the password was stored: store it with the new one.
                user.PasswordHash = hasher.HashPassword(user, password);
                users.Update(user);
            }

            return new AuthenticatedUser(user.Id, user.Username, user.Role);
        }

        /// <summary>A stored password that cannot even be read (a damaged record) counts as a wrong password, never as a crash.</summary>
        private PasswordVerificationResult Verify(AppUser user, string password)
        {
            try
            {
                return hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        public bool EnsureFirstUser(string username, string password, string role)
        {
            if (users.Any())
            {
                return false;
            }

            EnsureUser(username, password, role);
            return true;
        }

        public void EnsureUser(string username, string password, string role)
        {
            if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required.", nameof(username));
            if (string.IsNullOrWhiteSpace(role)) throw new ArgumentException("Role is required.", nameof(role));
            if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
            {
                throw new ArgumentException($"Password must be at least {MinPasswordLength} characters.", nameof(password));
            }

            var normalized = NormalizeUsername(username);
            if (users.GetByUsername(normalized) != null)
            {
                return;
            }

            var user = new AppUser { Username = normalized, Role = role };
            user.PasswordHash = hasher.HashPassword(user, password);
            users.Create(user);
        }
    }
}
