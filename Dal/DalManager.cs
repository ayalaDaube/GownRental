using Dal.Api;
using Dal.Models;
using Dal.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dal
{
    public class DalManager : IDal
    {
        public IRentalDetails RentalDetails { get; }

        public IRentals Rentals { get; }
        
        public IGownDetails gownDetail { get; }

        public IGownModel gownModel { get; }

        public DalManager()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddDbContext<DataManager>();
            services.AddScoped<IRentalDetails, RentalDetailsService>();
            services.AddScoped<IRentals,RentalService>();
            services.AddScoped<IGownModel, GownModelService>();
            services.AddScoped<IGownDetails, GownDetailsService>();

            ServiceProvider provider = services.BuildServiceProvider();
            Rentals=provider.GetRequiredService<IRentals>();
            RentalDetails= provider.GetRequiredService<IRentalDetails>();
            gownModel = provider.GetRequiredService<IGownModel>();
            gownDetail = provider.GetRequiredService<IGownDetails>();

        }

    }
}
