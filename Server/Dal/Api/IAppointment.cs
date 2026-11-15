using Dal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dal.Api
{
    public interface IAppointment
    {
        List<Appointment> GetAllAppointments();

        /// <summary>The free slots from the given moment on, ordered by time, filtered by the database.</summary>
        List<Appointment> GetFreeSlotsFrom(DateOnly date, TimeOnly time);
        List<Appointment> GetAppointmentByDate(DateOnly date);
        Appointment? GetAppointmentById(int id);
        Appointment? GetByDateAndHour(DateOnly date, TimeOnly hour);
        void DeleteAppointment(int id);
        void CreateAppointment(Appointment a);
        
    }
}
