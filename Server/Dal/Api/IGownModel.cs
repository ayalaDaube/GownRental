using Dal.Models;

namespace Dal.Api
{
    public interface IGownModel
    {
        List<GownModel> GetGownModels();
        GownModel? GetGownModelById(int id);
        int GetNextId();
        void DeleteGownModel(int id);
        void CreateGownModel(GownModel gm);
        void UpdateGownModel(GownModel gm);
    }
}
