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
        public IBLGowns BLGowns { get; }
        //qqשנשתמש. להוסיף את כל הסרויסים
        public BLManager()
        {
           ServiceCollection services = new ServiceCollection();
            services.AddSingleton <IDal,DalManager >();   
            services.AddScoped<IBLGowns, BLGownModelService>(); 
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            BLGowns= serviceProvider.GetService<IBLGowns>();    

            //BLGownModel=new BLGownModelService(dal.GownModel)
        }
    }
}
