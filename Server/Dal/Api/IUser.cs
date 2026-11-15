using Dal.Models;

namespace Dal.Api
{
    public interface IUser
    {
        AppUser? GetByUsername(string username);
        bool Any();
        void Create(AppUser user);
        void Update(AppUser user);
    }
}
