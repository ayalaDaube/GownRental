using Bl.Api;
using Bl.Exceptions;
using Bl.Models;
using Dal.Api;
using Dal.Models;

namespace Bl.Services
{
    public class BLAppointmentService : IBLAppointment
    {
        public const int MaxPeople = 10;

        private readonly IDal dal;
        private readonly ICustomer customers;
        private readonly IAppointment appointments;
        private readonly CustomerRegistrar registrar;

        public BLAppointmentService(IDal dal)
        {
            this.dal = dal;
            appointments = dal.Appointment;
            customers = dal.Customer;
            registrar = new CustomerRegistrar(dal.Customer);
        }

        /// <summary>
        /// Books a slot. The slot must exist, lie in the future and still be free; a customer who is new is
        /// registered. All of it is saved together, so a refused booking changes nothing.
        /// </summary>
        public BookingConfirmation Create(AppointmentFullDetaills request)
        {
            if (request == null)
            {
                throw new InvalidRequestException("The booking details are required.");
            }

            var customerId = CustomerRules.NormalizeId(request.CustomerId);
            var people = request.NumberOfPeople ?? 1;
            if (people < 1 || people > MaxPeople)
            {
                throw new InvalidRequestException($"The number of people must be between 1 and {MaxPeople}.");
            }

            return dal.RunInTransaction(() =>
            {
                var slot = appointments.GetByDateAndHour(request.AppointmentDate, request.Hour)
                    ?? throw new NotFoundException(
                        $"There is no appointment on {request.AppointmentDate:yyyy-MM-dd} at {request.Hour:HH:mm}.");

                if (!IsInTheFuture(slot))
                {
                    throw new InvalidRequestException("This appointment time has already passed.");
                }
                if (slot.IsAvailable != true)
                {
                    throw new ConflictException("This appointment is already booked. Please choose another time.");
                }

                registrar.EnsureExists(customerId, request.CustomerName, request.CustomerPhone, request.CustomerEmail);

                appointments.CreateAppointment(new Appointment
                {
                    Id = slot.Id,
                    Date = slot.Date,
                    Hour = slot.Hour,
                    CustomerId = customerId,
                    NumberOfPeople = people,
                    IsAvailable = false,
                });

                return new BookingConfirmation(slot.Id, slot.Date, slot.Hour, people);
            });
        }

        /// <summary>Cancels a booking. The slot keeps its date and hour and becomes free again.</summary>
        public void DeleteAppointment(int id)
        {
            dal.RunInTransaction(() =>
            {
                var existing = appointments.GetAppointmentById(id)
                    ?? throw new NotFoundException($"Appointment {id} does not exist.");
                if (existing.IsAvailable == true)
                {
                    throw new ConflictException($"Appointment {id} is not booked.");
                }

                appointments.DeleteAppointment(id);
            });
        }

        public List<AppointmentFullDetaills> GetFullAppointments()
        {
            return ToDtos(appointments.GetAllAppointments());
        }

        public AppointmentFullDetaills GetAppointmentId(int id)
        {
            var existing = appointments.GetAppointmentById(id)
                ?? throw new NotFoundException($"Appointment {id} does not exist.");
            return ToDtos(new List<Appointment> { existing }).Single();
        }

        public List<AppointmentFullDetaills> GetAppointmentDate(DateOnly date)
        {
            return ToDtos(appointments.GetAppointmentByDate(date));
        }

        public List<AppointmentFullDetaills> GetAvailableAppointments()
        {
            // Only what a visitor needs to pick a time: no customer data, and nothing that already passed.
            return appointments.GetAllAppointments()
                .Where(a => a.IsAvailable == true && IsInTheFuture(a))
                .OrderBy(a => a.Date).ThenBy(a => a.Hour)
                .Select(a => new AppointmentFullDetaills
                {
                    Id = a.Id,
                    AppointmentDate = a.Date,
                    Hour = a.Hour,
                    IsAvailable = a.IsAvailable,
                })
                .ToList();
        }

        private static bool IsInTheFuture(Appointment slot)
        {
            return slot.Date.ToDateTime(slot.Hour) > DateTime.Now;
        }

        private List<AppointmentFullDetaills> ToDtos(List<Appointment> slots)
        {
            var byId = customers.GetAllCustomers().ToDictionary(c => c.Id);
            return slots
                .OrderBy(a => a.Date).ThenBy(a => a.Hour)
                .Select(a =>
                {
                    byId.TryGetValue(a.CustomerId ?? string.Empty, out var customer);
                    return new AppointmentFullDetaills
                    {
                        Id = a.Id,
                        AppointmentDate = a.Date,
                        Hour = a.Hour,
                        CustomerId = a.CustomerId!,
                        NumberOfPeople = a.NumberOfPeople,
                        IsAvailable = a.IsAvailable,
                        CustomerName = customer?.Name,
                        CustomerPhone = customer?.Phone,
                        CustomerEmail = customer?.Email,
                    };
                })
                .ToList();
        }
    }
}
