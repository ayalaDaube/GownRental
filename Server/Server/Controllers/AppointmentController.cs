using Bl.Api;
using Bl.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IBLAppointment bl;

        public AppointmentController(IBl bl)
        {
            this.bl = bl.BLAppointment;
        }

        /// <summary>All appointment slots, booked and free, with the customer of each booking (staff only).</summary>
        [HttpGet]
        public List<AppointmentFullDetails> GetAllAppointments()
        {
            return bl.GetAllAppointments();
        }

        [HttpGet("{id:int}")]
        public ActionResult<AppointmentFullDetails> GetAppointmentById(int id)
        {
            return bl.GetAppointmentById(id);
        }

        [HttpGet("GetByDate/{date}")]
        public List<AppointmentFullDetails> GetAppointmentByDate(DateOnly date)
        {
            return bl.GetAppointmentsByDate(date);
        }

        /// <summary>Public: the free slots that are still in the future, so a visitor can pick one.</summary>
        [AllowAnonymous]
        [HttpGet("getAvailableAppointments")]
        public List<AppointmentFullDetails> GetAvailableAppointments()
        {
            return bl.GetAvailableAppointments();
        }

        /// <summary>
        /// Public: books a free slot. Answers 404 for an unknown slot, 409 when it is already booked
        /// and 400 for invalid details. The answer holds no customer details.
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public ActionResult<BookingConfirmation> Post([FromBody] AppointmentFullDetails newAppointment)
        {
            var confirmation = bl.Create(newAppointment);
            return StatusCode(StatusCodes.Status201Created, confirmation);
        }

        /// <summary>Cancels a booking (staff only); the slot becomes free again.</summary>
        [HttpDelete("{id:int}")]
        public IActionResult Cancel(int id)
        {
            bl.DeleteAppointment(id);
            return NoContent();
        }
    }
}
