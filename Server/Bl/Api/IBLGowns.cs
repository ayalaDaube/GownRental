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
        ModelFullDetaills createGown(NewGownRequest request);
            List<ModelFullDetaills> getFullGowns();
            ModelFullDetaills getGownById(int id);
            ModelFullDetaills updateGownSize(int gownId, UpdateSizeRequest request);
            ModelFullDetaills updateGownModel(int modelId, UpdateModelRequest request);
            void deleteGown(int id);
            void deleteGownModel(int modelId);
        List<ModelGrouped> markingTheOccupieByDate(DateOnly date);
        List<ModelGrouped> GetModelGroupeds();

    }
}
