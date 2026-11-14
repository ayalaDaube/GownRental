using Dal.Api;
using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Services
{
    public class GownModelService : IGownModel
    {
        DataManager dataManager;
        public GownModelService(DataManager dataManager)
        {
            this.dataManager = dataManager;
        }
        public void createGownModel(GownModel gm)
        {
            var existing = this.dataManager.GownModels.Find(gm.Id);
            if (existing != null)
            {
                existing.Name = gm.Name;
                existing.Color = gm.Color;
                existing.Description = gm.Description;
                existing.ImageUrl = gm.ImageUrl;
            }
            else
            {
                this.dataManager.GownModels.Add(gm);
            }
            this.dataManager.SaveChanges();
        }

        public void DeleteGownModel(int id)
        {
            GownModel gown = this.dataManager.GownModels.ToList().Find(x => x.Id == id);
            if (gown != null)
            {
                this.dataManager.GownModels.ToList().Remove(gown);
                this.dataManager.SaveChanges();
            }

        }

        public GownModel GetGownModelById(int id)
        {
            GownModel gown = this.dataManager.GownModels.ToList().Find(x => x.Id == id);
            if (gown == null)
            {
                throw new ArgumentNullException("this gown model does not exist");
            }
            return gown;
        }

        public List<GownModel> GetGownModels()
        {
            return this.dataManager.GownModels.ToList();
        }

        public void updateGownModel(GownModel gm)
        {
            GownModel old = this.dataManager.GownModels.ToList().Find(x => x.Id == gm.Id);
            if (old == null)
            {
                throw new ArgumentNullException("this gown model does not exist. you can't update it ");
            }
            old.Description = gm.Description;
            old.Color = gm.Color;
            old.Name = gm.Name;
            old.ImageUrl = gm.ImageUrl;
            this.dataManager.SaveChanges();
        }
    }
}
