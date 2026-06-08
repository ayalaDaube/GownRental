using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dal.Models;

namespace Dal.Api
{
    public interface ICustomer
    {
        List<Customer> GetAllCustomers();
        Customer GetCustomerById(string id);
        void DeleteCustomer(string id);
        void CreateCustomer(Customer c);
        void UpdateCustomer(Customer c);
    }
}
