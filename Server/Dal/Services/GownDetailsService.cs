using Dal.Api;
using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Services
{
    public class GownDetailsService : IGownDetails
    {

        DataManager dataManager;
        public GownDetailsService(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }
        public void createGownDetails(ModelDetails gd)
        {
            var model = this.dataManager.GownModels.Find(gd.ModelId);
            if (model == null)
            {
                throw new ArgumentException("ModelId does not exist.");
            }
            this.dataManager.ModelDetails.Add(gd);
            this.dataManager.SaveChanges();
        }

        public void DeleteGownDetails(int id)
        {
            ModelDetails gown = this.dataManager.ModelDetails.ToList().Find(x => x.GownId == id);
            if (gown != null)
            {
                this.dataManager.ModelDetails.Remove(gown);
                this.dataManager.SaveChanges();
            }
        }

        public List<ModelDetails> GetGownDetails()
        {
            return this.dataManager.ModelDetails.ToList();
        }

        public ModelDetails GetGownDetailsById(int id)
        {
            ModelDetails gown = this.dataManager.ModelDetails.ToList().Find(x => x.GownId == id);
            if (gown == null)
            {
                throw new ArgumentNullException("this detail of the gown model does not exist");
            }
            return gown;
        }

        public void updateGownDetails(ModelDetails gd)
        {
            ModelDetails old = this.dataManager.ModelDetails.ToList().Find(x => x.GownId == gd.GownId);
            if (old == null)
            {
                throw new ArgumentNullException("this detail of the gown model does not exist. you can't update it ");
            }
            old.Price = gd.Price;
            old.ModelId = gd.ModelId;
            old.Size = gd.Size;
            old.IsAvailable = gd.IsAvailable;
            this.dataManager.SaveChanges();
        }
    }
}
