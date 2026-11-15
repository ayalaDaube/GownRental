using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Dal.Services
{
    public class AppointmentService : IAppointment
    {
        DataManager datamanager;
        public AppointmentService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }

        public void CreateAppointment(Appointment a)
        {
            if (a == null) throw new ArgumentNullException(nameof(a));

            // Try to find existing appointment by Id and update it instead of adding a duplicate
            var existing = this.datamanager.Appointments.FirstOrDefault(x => x.Id == a.Id);
            if (existing != null)
            {
                existing.Date = a.Date;
                existing.CustomerId = a.CustomerId;
                existing.Hour = a.Hour;
                existing.NumberOfPeople = a.NumberOfPeople;
                existing.IsAvailable = a.IsAvailable;
            }
            else
            {
                // No matching Id found: add as new
                this.datamanager.Appointments.Add(a);
            }

            // Persist the change immediately so callers (BL / controller) see the updated DB state
            this.datamanager.SaveChanges();
        }

        public void DeleteAppointment(int id)
        {
            var app = this.datamanager.Appointments.FirstOrDefault(x => x.Id == id);
            if (app != null)
            {
                // Keep Id, Date and Hour; clear customer-specific details
                app.CustomerId = null;
                app.NumberOfPeople = null;
                app.IsAvailable = true;
                this.datamanager.SaveChanges();
            }
        }

        public List<Appointment> GetAllAppointments()
        {
            return this.datamanager.Appointments.AsNoTracking().ToList();
        }

        public List<Appointment> GetFreeSlotsFrom(DateOnly date, TimeOnly time)
        {
            return this.datamanager.Appointments
                .AsNoTracking()
                .Where(a => a.IsAvailable == true && (a.Date > date || (a.Date == date && a.Hour > time)))
                .OrderBy(a => a.Date).ThenBy(a => a.Hour)
                .ToList();
        }

        public List<Appointment> GetAppointmentByDate(DateOnly date)
        {
            return this.datamanager.Appointments.AsNoTracking().Where(x => x.Date == date).ToList();
        }

        public Appointment? GetAppointmentById(int id)
        {
            return datamanager.Appointments.Find(id);
        }

        public Appointment? GetByDateAndHour(DateOnly date, TimeOnly hour)
        {
            return datamanager.Appointments.FirstOrDefault(x => x.Date == date && x.Hour == hour);
        }

  


    }



}
    
