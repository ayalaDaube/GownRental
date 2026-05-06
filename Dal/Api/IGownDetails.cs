using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IGownDetails
    {
        List<ModelDetails> GetGownDetails();
        ModelDetails GetGownDetailsById(int id);
        void DeleteGownDetails(int id);
        void createGownDetails(ModelDetails gd);
        void updateGownDetails(ModelDetails gd);
    }
}
