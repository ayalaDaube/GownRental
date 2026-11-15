using Dal.Api;
using Dal.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Services
{
    public class CustomerService : ICustomer
    {
        private readonly DataManager datamanager;

        public CustomerService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }

        public bool Exists(string id)
        {
            return datamanager.Customers.Any(c => c.Id == id);
        }

        public List<Customer> GetByIds(IEnumerable<string> ids)
        {
            var wanted = ids.Distinct().ToList();
            return datamanager.Customers.AsNoTracking().Where(c => wanted.Contains(c.Id)).ToList();
        }

        public void CreateCustomer(Customer c)
        {
            ArgumentNullException.ThrowIfNull(c);
            datamanager.Customers.Add(c);
            datamanager.SaveChanges();
        }
    }
}
