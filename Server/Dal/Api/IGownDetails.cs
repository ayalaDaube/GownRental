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
        void createGownDetails(ModelDetails gd);
        void updateGownDetails(ModelDetails gd);
    }
}
