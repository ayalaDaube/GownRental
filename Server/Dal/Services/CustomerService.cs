using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dal.Api;
using Dal.Models;

namespace Dal.Services
{
    public class CustomerService : ICustomer
    {
        DataManager datamanager;
        public CustomerService(DataManager datamanager)
        {
            this.datamanager = datamanager;
        }
        public void CreateCustomer(Customer c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));                  
            try
            {
                this.datamanager.Customers.Add(c);
                this.datamanager.SaveChanges(); 
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create customer.", ex);
            }
        }

        public void DeleteCustomer(string id)
        {
            try
            {
                Customer customer = datamanager.Customers.ToList().Find(x => x.Id == id);
                if (customer != null)
                {
                    this.datamanager.Customers.Remove(customer);
                    this.datamanager.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to delete customer '{id}'.", ex);
            }
        }

        public bool Exists(string id)
        {
            return this.datamanager.Customers.Any(c => c.Id == id);
        }

        public List<Customer> GetAllCustomers()
        {
            return this.datamanager.Customers.ToList();
        }

        public Customer GetCustomerById(string id)
        {
            Customer customer = datamanager.Customers.ToList().Find(x => x.Id == id);
            if (customer == null)
            {
                throw new ArgumentNullException("This customer doesn't exist");
            }
            return customer;
        }

        public void UpdateCustomer(Customer c)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            try
            {
                Customer old = datamanager.Customers.ToList().Find(x => x.Id == c.Id);
                if (old == null)
                {
                    throw new ArgumentNullException("this customer does not exist. you can't update it ");
                }

                old.Name = c.Name;
                old.Phone = c.Phone;
                old.Email = c.Email;

                this.datamanager.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to update customer '{c?.Id}'.", ex);
            }
        }
    }
}
