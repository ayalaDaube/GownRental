using Bl;
using Bl.Api;
using Bl.Models;
using Dal.Models;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
        IBLRentals bl;

        public RentalsController(IBl  bl)
        {
            this.bl = bl.BLRentals; 
        }
        // GET: api/<RentalsController>
        [HttpGet]
        public List<RentalFullDetaills> GetAllGownRentals()
        {
            return bl.GetFullRentals();
        }

        // GET api/<RentalsController>/5
        [HttpGet("{id}")]
        public RentalFullDetaills GetRental(int id)
        {
           
            return bl.GetRentalById(id);
        }

        [Route("returning")]
        [HttpGet]
        public List<RentalFullDetaills> returningCustomers()
        {
            return bl.returningCustomers();
        }

        [Route("renting")]
        [HttpGet]
        public List<RentalFullDetaills> rentingCustomers()
        {
            return bl.rentingCustomers();
        }
        // POST api/<RentalsController>
        [HttpPost]
        public IActionResult Post([FromBody] RentalFullDetaills newRental)
        {
            try
            {
                bl.CreateRental(newRental);
                return Ok(new { success = true, message = "Rental created successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // PUT api/<RentalsController>/5
        [HttpPut]
        public IActionResult Put([FromBody] RentalFullDetaills updatedRental)
        {
            try
            {
                bl.UpdateRental(updatedRental);
                return Ok(new { success = true, message = "Rental updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // DELETE api/<RentalsController>/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                bl.DeleteRental(id);
                return Ok(new { success = true, message = "Rental deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = "Failed to delete rental", details = ex.Message });
            }
        }
    }
}
