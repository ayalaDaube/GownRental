using System.Text.RegularExpressions;
using Bl.Exceptions;

namespace Bl
{
    /// <summary>
    /// Validation and clean-up of customer details, so the same rules apply wherever a customer
    /// can be entered (rentals, appointments, orders).
    /// </summary>
    public static class CustomerRules
    {
        public const int MaxNameLength = 60;
        public const int MaxEmailLength = 100;

        private static readonly Regex IdFormat = new("^[A-Z0-9]{5,9}$", RegexOptions.Compiled);
        private static readonly Regex PhoneFormat = new(@"^\+?[0-9]{9,13}$", RegexOptions.Compiled);
        private static readonly Regex EmailFormat = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);

        /// <summary>An id number or passport number: 5 to 9 letters or digits, stored in capitals.</summary>
        public static string NormalizeId(string? id)
        {
            var value = id?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidRequestException("A customer id is required.");
            }
            if (!IdFormat.IsMatch(value))
            {
                throw new InvalidRequestException("The customer id must be 5 to 9 letters or digits.");
            }
            return value;
        }

        public static string NormalizeName(string? name)
        {
            var value = name?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidRequestException("A customer name is required.");
            }
            if (value.Length > MaxNameLength)
            {
                throw new InvalidRequestException($"The customer name can have at most {MaxNameLength} characters.");
            }
            return value;
        }

        /// <summary>A phone number of 9 to 13 digits with an optional leading +; spaces, dashes and brackets are dropped.</summary>
        public static string NormalizePhone(string? phone)
        {
            var value = phone == null ? null : Regex.Replace(phone, @"[\s\-()]", "");
            if (string.IsNullOrEmpty(value) || !PhoneFormat.IsMatch(value))
            {
                throw new InvalidRequestException("The phone number must have 9 to 13 digits.");
            }
            return value;
        }

        /// <summary>The email is optional: blank becomes null, anything else must look like an address.</summary>
        public static string? NormalizeEmail(string? email)
        {
            var value = email?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            if (value.Length > MaxEmailLength || !EmailFormat.IsMatch(value))
            {
                throw new InvalidRequestException("The email address is not valid.");
            }
            return value;
        }
    }
}
