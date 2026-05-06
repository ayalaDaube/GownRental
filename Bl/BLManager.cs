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
        public BLManager()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddSingleton<IDal,DalManager>();
            services.AddScoped<IBLRentals, BLRentalService>();
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            BLRentals = serviceProvider.GetService<IBLRentals>();
           
          }
    }
}
