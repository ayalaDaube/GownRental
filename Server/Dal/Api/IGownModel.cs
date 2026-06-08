using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IGownModel
    {
        List<GownModel> GetGownModels();
        GownModel GetGownModelById(int id);
        void DeleteGownModel(int id);
        void createGownModel(GownModel gm);
        void updateGownModel(GownModel gm);
    }
}
