using Bl.Models;

namespace Bl.Api
{
    public interface IBLOrders
    {
        /// <summary>Rents all the gowns of the order, or none of them if any one cannot be rented.</summary>
        OrderResult PlaceOrder(OrderRequest request);
    }
}
