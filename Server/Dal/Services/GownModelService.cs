using Dal.Api;
using Dal.Models;

namespace Dal.Services
{
    public class GownModelService : IGownModel
    {
        private readonly DataManager dataManager;

        public GownModelService(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }

        public void createGownModel(GownModel gm)
        {
            var existing = dataManager.GownModels.Find(gm.Id);
            if (existing != null)
            {
                existing.Name = gm.Name;
                existing.Color = gm.Color;
                existing.Description = gm.Description;
                existing.ImageUrl = gm.ImageUrl;
            }
            else
            {
                dataManager.GownModels.Add(gm);
            }
            dataManager.SaveChanges();
        }

        public void DeleteGownModel(int id)
        {
            var gown = dataManager.GownModels.Find(id);
            if (gown != null)
            {
                dataManager.GownModels.Remove(gown);
                dataManager.SaveChanges();
            }
        }

        public int GetNextId()
        {
            return (dataManager.GownModels.Max(m => (int?)m.Id) ?? 0) + 1;
        }

        public GownModel? GetGownModelById(int id)
        {
            return dataManager.GownModels.Find(id);
        }

        public List<GownModel> GetGownModels()
        {
            return dataManager.GownModels.ToList();
        }

        public void updateGownModel(GownModel gm)
        {
            var old = dataManager.GownModels.Find(gm.Id);
            if (old == null)
            {
                throw new ArgumentNullException("this gown model does not exist. you can't update it ");
            }
            old.Description = gm.Description;
            old.Color = gm.Color;
            old.Name = gm.Name;
            old.ImageUrl = gm.ImageUrl;
            dataManager.SaveChanges();
        }
    }
}
