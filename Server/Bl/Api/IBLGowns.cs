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
        ModelFullDetails CreateGown(NewGownRequest request);
            List<ModelFullDetails> GetFullGowns();
            ModelFullDetails? GetGownById(int id);
            ModelFullDetails UpdateGownSize(int gownId, UpdateSizeRequest request);
            ModelFullDetails UpdateGownModel(int modelId, UpdateModelRequest request);
            void DeleteGown(int id);
            void DeleteGownModel(int modelId);
        List<ModelGrouped> GetGroupedGownsForDate(DateOnly date);
        List<ModelGrouped> GetGroupedGowns();

    }
}
