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
        public void Post([FromBody] RentalFullDetaills newRental)
        {
            bl.CreateRental(newRental);
        }

        // PUT api/<RentalsController>/5
        [HttpPut]
        public void Put([FromBody] RentalFullDetaills updatedRental)
        {
            bl.UpdateRental(updatedRental);
        }

        // DELETE api/<RentalsController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            bl.DeleteRental(id);
        }
    }
}
