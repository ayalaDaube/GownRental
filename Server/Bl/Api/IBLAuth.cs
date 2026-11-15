using Bl.Models;

namespace Bl.Api
{
    public interface IBLAuth
    {
        /// <summary>Returns the user when the username and password match, otherwise null.</summary>
        AuthenticatedUser? ValidateCredentials(string username, string password);

        /// <summary>Creates the account if the username does not exist yet; an existing account is left untouched.</summary>
        void EnsureUser(string username, string password, string role);

        /// <summary>Creates the account only when there are no accounts at all (the first start of a new installation). Returns whether it was created.</summary>
        bool EnsureFirstUser(string username, string password, string role);
    }
}
