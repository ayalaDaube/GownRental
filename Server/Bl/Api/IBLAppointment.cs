using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bl.Models;

namespace Bl.Api
{
    public interface IBLAppointment
    {
        void Create(AppointmentFullDetaills appointment);
        List<AppointmentFullDetaills> GetFullAppointments();
        AppointmentFullDetaills? GetAppointmentId(int id);
        List<AppointmentFullDetaills>? GetAppointmentDate(DateOnly date);
        List<AppointmentFullDetaills> GetAvailableAppointments();
        //void UpdateAppointment(AppointmentFullDetaills appointment);
        void DeleteAppointment(int id);
       
    }
}
