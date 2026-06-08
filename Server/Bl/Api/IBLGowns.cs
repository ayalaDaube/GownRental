using Bl.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Api
{
    public interface IBLGowns
    {
        void createGown(ModelFullDetaills model);
            List<ModelFullDetaills> getFullGowns();
            ModelFullDetaills getGownById(int id);
            void updateGown(ModelFullDetaills model);
            void deleteGown(int id);
        List<ModelGrouped> markingTheOccupieByDate(DateOnly date);
        List<ModelGrouped> GetModelGroupeds();

    }
}
