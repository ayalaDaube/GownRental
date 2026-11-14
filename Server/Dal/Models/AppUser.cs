namespace Dal.Models;

/// <summary>
/// A person who can sign in to the management area (for example the shop secretary).
/// Only a salted hash of the password is stored, never the password itself.
/// </summary>
public partial class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
