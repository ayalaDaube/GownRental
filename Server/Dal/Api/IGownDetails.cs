using Dal.Models;

namespace Dal.Api
{
    public interface IGownDetails
    {
        List<ModelDetails> GetGownDetails();
        ModelDetails? GetGownDetailsById(int id);
        List<ModelDetails> GetByModel(int modelId);
        int GetNextId();
        void DeleteGownDetails(int id);
        void CreateGownDetails(ModelDetails gd);
        void UpdateGownDetails(ModelDetails gd);
    }
}
