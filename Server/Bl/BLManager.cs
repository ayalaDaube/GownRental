using Bl.Api;
using Bl.Services;
using Dal;
using Dal.Api;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl
{
    public class BLManager:IBl 
    {
        public IBLRentals BLRentals { get; }
         public IBLGowns BLGowns { get; }
        public BLManager()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IDal,DalManager>();
            services.AddScoped<IBLRentals, BLRentalService>();
            services.AddScoped<IBLGowns, BLGownModelService>(); 
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            BLRentals = serviceProvider.GetService<IBLRentals>();
            BLGowns= serviceProvider.GetService<IBLGowns>();  
          }
    }
}
