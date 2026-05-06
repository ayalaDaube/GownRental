using Dal.Api;
using Dal.Models;
using Dal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dal
{
    public class DalManager : IDal
    {
        public IGownDetails gownDetail { get; }

        public IGownModel gownModel { get; }

        public DalManager()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddDbContext<DataManager>();
            services.AddSingleton<IGownModel, GownModelService>();
            services.AddSingleton<IGownDetails, GownDetailsService>();

            ServiceProvider provider = services.BuildServiceProvider();
            gownModel = provider.GetRequiredService<IGownModel>();
            gownDetail = provider.GetRequiredService<IGownDetails>();

            // DataManager db = new();
            // Products=new ProductService(db);


            //  Orders = new OrdersService(db);
        }
    }
}
