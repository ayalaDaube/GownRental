using Bl.Api;
using Bl.Models;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RentalsController : ControllerBase
    {
        private readonly IBLRentals bl;

        public RentalsController(IBl bl)
        {
            this.bl = bl.BLRentals;
        }

        [HttpGet]
        public List<RentalFullDetails> GetAllRentals()
        {
            return bl.GetFullRentals();
        }

        [HttpGet("{id:int}")]
        public ActionResult<RentalFullDetails> GetRental(int id)
        {
            return bl.GetRentalById(id);
        }

        /// <summary>Rentals whose gowns come back to the shop today (events from three days ago).</summary>
        [HttpGet("returning")]
        public List<RentalFullDetails> GetReturningRentals()
        {
            return bl.GetReturningRentals();
        }

        /// <summary>Rentals whose gowns customers pick up today (events in three days).</summary>
        [HttpGet("renting")]
        public List<RentalFullDetails> GetPickupRentals()
        {
            return bl.GetPickupRentals();
        }

        /// <summary>Rents a gown. Answers 404 for an unknown gown or customer and 409 when the gown is not free.</summary>
        [HttpPost]
        public ActionResult<RentalFullDetails> Post([FromBody] RentalFullDetails newRental)
        {
            var created = bl.CreateRental(newRental);
            return CreatedAtAction(nameof(GetRental), new { id = created.RentalId }, created);
        }

        [HttpPut]
        public ActionResult<RentalFullDetails> Put([FromBody] RentalFullDetails updatedRental)
        {
            return bl.UpdateRental(updatedRental);
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            bl.DeleteRental(id);
            return NoContent();
        }
    }
}
