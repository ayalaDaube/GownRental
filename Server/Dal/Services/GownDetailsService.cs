using Dal.Api;
using Dal.Models;

namespace Dal.Services
{
    public class GownDetailsService : IGownDetails
    {
        private readonly DataManager dataManager;

        public GownDetailsService(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }

        public void createGownDetails(ModelDetails gd)
        {
            var model = dataManager.GownModels.Find(gd.ModelId);
            if (model == null)
            {
                throw new ArgumentException("ModelId does not exist.");
            }
            dataManager.ModelDetails.Add(gd);
            dataManager.SaveChanges();
        }

        public void DeleteGownDetails(int id)
        {
            var gown = dataManager.ModelDetails.Find(id);
            if (gown != null)
            {
                dataManager.ModelDetails.Remove(gown);
                dataManager.SaveChanges();
            }
        }

        public List<ModelDetails> GetGownDetails()
        {
            return dataManager.ModelDetails.ToList();
        }

        public ModelDetails? GetGownDetailsById(int id)
        {
            return dataManager.ModelDetails.Find(id);
        }

        public int GetNextId()
        {
            return (dataManager.ModelDetails.Max(d => (int?)d.GownId) ?? 0) + 1;
        }

        public List<ModelDetails> GetByModel(int modelId)
        {
            return dataManager.ModelDetails.Where(d => d.ModelId == modelId).ToList();
        }

        public void updateGownDetails(ModelDetails gd)
        {
            var old = dataManager.ModelDetails.Find(gd.GownId);
            if (old == null)
            {
                throw new ArgumentNullException("this detail of the gown model does not exist. you can't update it ");
            }
            old.Price = gd.Price;
            old.ModelId = gd.ModelId;
            old.Size = gd.Size;
            old.IsActive = gd.IsActive;
            dataManager.SaveChanges();
        }
    }
}
