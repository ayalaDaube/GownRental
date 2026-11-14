using Bl.Exceptions;
using Dal.Api;
using Dal.Models;

namespace Bl.Services
{
    /// <summary>
    /// Makes sure a customer exists before something is booked for them.
    /// Details of an existing customer are never changed here: this is also used by public requests,
    /// and typing someone's id number must not let a stranger overwrite that person's name or phone.
    /// </summary>
    internal class CustomerRegistrar
    {
        private readonly ICustomer customers;

        public CustomerRegistrar(ICustomer customers)
        {
            this.customers = customers;
        }

        /// <param name="normalizedId">An id already cleaned with <see cref="CustomerRules.NormalizeId"/>.</param>
        public void EnsureExists(string normalizedId, string? name, string? phone, string? email)
        {
            if (customers.Exists(normalizedId))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
            {
                throw new InvalidRequestException(
                    $"Customer {normalizedId} is not registered yet. Enter a name and a phone number to register them.");
            }

            customers.CreateCustomer(new Customer
            {
                Id = normalizedId,
                Name = CustomerRules.NormalizeName(name),
                Phone = CustomerRules.NormalizePhone(phone),
                Email = CustomerRules.NormalizeEmail(email),
            });
        }
    }
}
