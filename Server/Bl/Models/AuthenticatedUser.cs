namespace Bl.Models
{
    /// <summary>The identity of a user whose credentials were verified.</summary>
    public record AuthenticatedUser(int Id, string Username, string Role);
}
