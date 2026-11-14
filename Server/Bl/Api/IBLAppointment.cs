using Bl.Models;

namespace Bl.Api
{
    public interface IBLAppointment
    {
        /// <summary>Books a free slot for a customer, registering the customer if new.</summary>
        BookingConfirmation Create(AppointmentFullDetaills appointment);
        List<AppointmentFullDetaills> GetFullAppointments();
        AppointmentFullDetaills GetAppointmentId(int id);
        List<AppointmentFullDetaills> GetAppointmentDate(DateOnly date);

        /// <summary>The slots that are free and still in the future.</summary>
        List<AppointmentFullDetaills> GetAvailableAppointments();

        /// <summary>Cancels a booking: the slot becomes free again.</summary>
        void DeleteAppointment(int id);
    }
}
