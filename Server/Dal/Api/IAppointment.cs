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
        List<Appointment> GetAppointmentByDate(DateOnly date);
        Appointment GetAppointmentById(int id);
        void DeleteAppointment(int id);
        void CreateAppointment(Appointment a);
        
    }
}
