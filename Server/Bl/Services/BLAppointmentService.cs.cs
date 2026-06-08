
using Bl.Api;
using Bl.Models;
using Dal.Api;
using Dal.Models;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bl.Services
{

    public class BLAppointmentService : IBLAppointment
    {

        ICustomer customer;
        IAppointment appointment;
        public BLAppointmentService(IDal A)
        {
            appointment = A.Appointment;
            customer = A.Customer;
        }

        public void Create(AppointmentFullDetaills fullAppointment)

        {
            if (fullAppointment == null) throw new ArgumentNullException(nameof(fullAppointment));

            try
            {
                // Find the existing appointment slot that the UI displayed and the user selected.
                var slot = appointment.GetAllAppointments()
                            .FirstOrDefault(a => a.Date == fullAppointment.AppointmentDate && a.Hour == fullAppointment.Hour);

                if (slot == null)
                {
                    throw new InvalidOperationException("Selected appointment slot not found.");
                }

                // Ensure customer exists; create only if not found
                var existingCustomer = customer.GetAllCustomers()
                                        .FirstOrDefault(c => c.Id == fullAppointment.CustomerId);

                if (existingCustomer == null)
                {
                    customer.CreateCustomer(new Customer()
                    {
                        Id = fullAppointment.CustomerId,
                        Name = fullAppointment.CustomerName,
                        Phone = fullAppointment.CustomerPhone,
                        Email = fullAppointment.CustomerEmail
                    });
                }
                else
                {
                    existingCustomer.Name = fullAppointment.CustomerName;
                    existingCustomer.Phone = fullAppointment.CustomerPhone;
                    existingCustomer.Email = fullAppointment.CustomerEmail;
                    customer.UpdateCustomer(existingCustomer);
                }

                // Update the selected appointment to assign the customer and mark as not available
                var updatedAppointment = new Appointment()
                {
                    Id = slot.Id,
                    Date = slot.Date,
                    Hour = slot.Hour,
                    CustomerId = fullAppointment.CustomerId,
                    NumberOfPeople = fullAppointment.NumberOfPeople,
                    IsAvailable = false
                };

                // This will update the existing slot (DAL now saves changes)
                appointment.CreateAppointment(updatedAppointment);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create appointment.", ex);
            }

        }

        public void DeleteAppointment(int id)
        {
            var oldAppointment = appointment.GetAppointmentById(id);
            if (oldAppointment == null)
            {
                throw new ArgumentNullException("This appointment details doesn't exist");
            }

            // DAL will clear the details but keep Id, Date and Hour
            appointment.DeleteAppointment(id);
        }

        public List<AppointmentFullDetaills>? GetAppointmentDate(DateOnly date)
        {
            
            return (from a in appointment.GetAllAppointments()
                    join c in customer.GetAllCustomers() on a.CustomerId equals c.Id
                    where a.Date == date
                    select new AppointmentFullDetaills()
                    {
                        Id=a.Id,
                        AppointmentDate = a.Date,
                        CustomerId = a.CustomerId,
                        Hour = a.Hour,
                        NumberOfPeople = a.NumberOfPeople,
                        IsAvailable = a.IsAvailable,
                        CustomerName = c.Name,
                        CustomerPhone = c.Phone,
                        CustomerEmail = c.Email
                    }).ToList();

        }
        public List<AppointmentFullDetaills>? GetAvailableAppointments()
        {
            return (from a in appointment.GetAllAppointments()
                    where a.IsAvailable == true 
                    select new AppointmentFullDetaills()
                    {
                        Id=a.Id,
                        AppointmentDate = a.Date,
                        CustomerId = a.CustomerId,
                        Hour = a.Hour,
                        NumberOfPeople = a.NumberOfPeople,
                        IsAvailable = a.IsAvailable,
                    }).ToList();
        }

        public AppointmentFullDetaills? GetAppointmentId(int id)
        {
            return (from a in appointment.GetAllAppointments()
                    join c in customer.GetAllCustomers() on a.CustomerId equals c.Id
                    where a.Id == id
                    select new AppointmentFullDetaills()
                    {
                        Id=a.Id,
                        AppointmentDate = a.Date,
                        CustomerId = a.CustomerId,
                        Hour = a.Hour,
                        NumberOfPeople = a.NumberOfPeople,
                        IsAvailable = a.IsAvailable,
                        CustomerName = c.Name,
                        CustomerPhone = c.Phone,
                        CustomerEmail = c.Email
                    }).FirstOrDefault();
        }

        public List<AppointmentFullDetaills> GetFullAppointments()
        {
            if (appointment.GetAllAppointments == null || customer.GetAllCustomers()==null)
            {
                throw new ArgumentNullException("No appointments or customers");
            }

                return (from a in appointment.GetAllAppointments()
                    join c in customer.GetAllCustomers() on a.CustomerId equals c.Id into cGroup
                    from c in cGroup.DefaultIfEmpty()
                    select new AppointmentFullDetaills()
                    {
                        Id=a.Id,
                        AppointmentDate = a.Date,
                        CustomerId = a.CustomerId,
                        Hour = a.Hour,
                        NumberOfPeople = a.NumberOfPeople,
                        IsAvailable = a.IsAvailable,
                        CustomerName = c != null ? c.Name : null,
                        CustomerPhone = c != null ? c.Phone : null,
                        CustomerEmail = c != null ? c.Email : null
                    }).ToList();


        }



        
    }
}

       
  

