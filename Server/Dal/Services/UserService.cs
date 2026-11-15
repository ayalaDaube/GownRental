using Dal.Api;
using Dal.Models;

namespace Dal.Services
{
    public class UserService : IUser
    {
        private readonly DataManager dataManager;

        public UserService(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }

        public AppUser? GetByUsername(string username)
        {
            // The column uses the database's case-insensitive collation, so "Secretary" finds "secretary".
            return dataManager.AppUsers.FirstOrDefault(u => u.Username == username);
        }

        public bool Any() => dataManager.AppUsers.Any();

        public void Create(AppUser user)
        {
            ArgumentNullException.ThrowIfNull(user);
            dataManager.AppUsers.Add(user);
            dataManager.SaveChanges();
        }

        public void Update(AppUser user)
        {
            ArgumentNullException.ThrowIfNull(user);
            dataManager.AppUsers.Update(user);
            dataManager.SaveChanges();
        }
    }
}
