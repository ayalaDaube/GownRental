using Dal.Models;

namespace Dal.Api
{
    public interface IGownModel
    {
        List<GownModel> GetGownModels();
        GownModel? GetGownModelById(int id);
        void DeleteGownModel(int id);
        void createGownModel(GownModel gm);
        void updateGownModel(GownModel gm);
    }
}
