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
        
        public DalManager()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddDbContext<DataManager>();
            services.AddScoped<IRentalDetails, RentalDetailsService>();
            services.AddScoped<IRentals,RentalService>();
          
            ServiceProvider provider = services.BuildServiceProvider();
            Rentals=provider.GetRequiredService<IRentals>();
            RentalDetails= provider.GetRequiredService<IRentalDetails>();
            // DataManager db = new();
            // Products=new ProductService(db);


            //  Orders = new OrdersService(db);
        }


    }
}
