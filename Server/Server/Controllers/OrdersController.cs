using Bl.Api;
using Bl.Models;
using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IBLOrders bl;

        public OrdersController(IBl bl)
        {
            this.bl = bl.BLOrders;
        }

        /// <summary>
        /// Rents several gowns to one customer for one event, all or nothing.
        /// Answers 404 for an unknown gown, 409 when a gown is not free, and 400 for invalid details.
        /// </summary>
        [HttpPost]
        public ActionResult<OrderResult> Post([FromBody] OrderRequest request)
        {
            var result = bl.PlaceOrder(request);
            return StatusCode(StatusCodes.Status201Created, result);
        }
    }
}
