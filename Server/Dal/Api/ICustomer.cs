using Dal.Models;

namespace Dal.Api
{
    public interface ICustomer
    {
        bool Exists(string id);
        List<Customer> GetByIds(IEnumerable<string> ids);
        void CreateCustomer(Customer c);
    }
}
