using Bl;
using Bl.Api;
using Dal;
using Dal.Api;
using Dal.Models;
using Dal.Seed;
using Dal.Services;
using Microsoft.EntityFrameworkCore;

namespace Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<DataManager>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
            builder.Services.AddScoped<IRentals, RentalService>();
            builder.Services.AddScoped<IRentalDetails, RentalDetailsService>();
            builder.Services.AddScoped<IGownModel, GownModelService>();
            builder.Services.AddScoped<IGownDetails, GownDetailsService>();
            builder.Services.AddScoped<IDal, DalManager>();
            builder.Services.AddScoped<IBl, BLManager>();
            builder.Services.AddScoped<Dal.Api.IAppointment, Dal.Services.AppointmentService>();
            builder.Services.AddScoped<Dal.Api.ICustomer, Dal.Services.CustomerService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowReact", policy =>
                {
                    policy.WithOrigins("http://localhost:3000")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // In development the database is created and filled with demo data on startup.
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<DataManager>();
                db.Database.Migrate();
                DemoDataSeeder.Seed(db, DateOnly.FromDateTime(DateTime.Today));
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowReact");

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
