using Bl;
using Bl.Exceptions;

namespace Server.Tests;

/// <summary>
/// What may be typed as a customer's id, name, phone and email. Every limit is checked on both sides.
/// The rules: an id or passport number of 5 to 9 letters or digits; a name of 1 to 60 characters;
/// a phone of 9 to 13 digits with an optional leading + (spaces, dashes and brackets are allowed);
/// an optional email that looks like an address and has at most 100 characters.
/// </summary>
public class CustomerRulesTests
{
    // ---- id

    [Theory]
    [InlineData("ABCDE", "ABCDE")]               // shortest allowed
    [InlineData("12345", "12345")]
    [InlineData("123456789", "123456789")]       // longest allowed, a typical id number
    [InlineData("a1b2c3d4e", "A1B2C3D4E")]       // capitals are used
    [InlineData("  c000001  ", "C000001")]       // spaces around are ignored
    [InlineData("12345\n", "12345")]
    public void A_valid_id_is_cleaned_to_capitals_without_spaces(string typed, string expected)
    {
        Assert.Equal(expected, CustomerRules.NormalizeId(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1234")]                         // one too short
    [InlineData("1234567890")]                   // one too long
    [InlineData("12 345")]                       // space inside
    [InlineData("12-345")]
    [InlineData("12.345")]
    [InlineData("12345_")]
    [InlineData("אבגדה")]                        // Hebrew letters
    [InlineData("ABCDÉ")]                        // accented letter
    [InlineData("١٢٣٤٥")]                        // Arabic-Indic digits
    [InlineData("12345🙂")]
    [InlineData("12345'; DROP TABLE Customer;--")]
    public void An_invalid_id_is_refused(string? typed)
    {
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizeId(typed));
    }

    // ---- name

    [Theory]
    [InlineData("א", "א")]
    [InlineData("Dana Levi", "Dana Levi")]
    [InlineData("  דנה לוי  ", "דנה לוי")]
    [InlineData("Mary-Jane O'Connor", "Mary-Jane O'Connor")]
    public void A_name_is_kept_as_typed_without_the_spaces_around_it(string typed, string expected)
    {
        Assert.Equal(expected, CustomerRules.NormalizeName(typed));
    }

    [Fact]
    public void A_name_of_sixty_characters_is_allowed_and_sixty_one_is_not()
    {
        Assert.Equal(60, CustomerRules.NormalizeName(new string('x', 60)).Length);
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizeName(new string('x', 61)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("      ")]
    public void A_name_is_required(string? typed)
    {
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizeName(typed));
    }

    // ---- phone

    [Theory]
    [InlineData("0501234567", "0501234567")]
    [InlineData("050-123-4567", "0501234567")]
    [InlineData("(050) 123 4567", "0501234567")]
    [InlineData("050 123 4567", "0501234567")]
    [InlineData("+972501234567", "+972501234567")]
    [InlineData("+972-50-123-4567", "+972501234567")]
    [InlineData("021234567", "021234567")]                    // nine digits: the shortest
    [InlineData("+9725012345678", "+9725012345678")]         // thirteen digits: the longest
    public void A_valid_phone_is_kept_without_separators(string typed, string expected)
    {
        Assert.Equal(expected, CustomerRules.NormalizePhone(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345678")]                  // eight digits
    [InlineData("12345678901234")]            // fourteen digits
    [InlineData("050123456a")]
    [InlineData("++972501234567")]
    [InlineData("050+1234567")]
    [InlineData("+")]
    [InlineData("---")]
    [InlineData("٠٥٠١٢٣٤٥٦٧")]                // Arabic-Indic digits
    [InlineData("050 123 4567 ext 5")]
    public void An_invalid_phone_is_refused(string? typed)
    {
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizePhone(typed));
    }

    // ---- email

    [Theory]
    [InlineData("dana@example.com", "dana@example.com")]
    [InlineData("  dana@example.com  ", "dana@example.com")]
    [InlineData("dana.levi+shop@mail.example.co.il", "dana.levi+shop@mail.example.co.il")]
    [InlineData("a@b.co", "a@b.co")]
    public void A_valid_email_is_kept(string typed, string expected)
    {
        Assert.Equal(expected, CustomerRules.NormalizeEmail(typed));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void The_email_is_optional(string? typed)
    {
        Assert.Null(CustomerRules.NormalizeEmail(typed));
    }

    [Theory]
    [InlineData("a@b")]
    [InlineData("@b.co")]
    [InlineData("a@.co")]
    [InlineData("a b@c.co")]
    [InlineData("a@@b.co")]
    [InlineData("a@b.c o")]
    [InlineData("plainaddress")]
    [InlineData("a@b.co<script>")]
    public void An_email_that_is_not_an_address_is_refused(string typed)
    {
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizeEmail(typed));
    }

    [Fact]
    public void An_email_of_one_hundred_characters_is_allowed_and_one_hundred_and_one_is_not()
    {
        var local = new string('a', 100 - "@example.com".Length);
        var allowed = local + "@example.com";

        Assert.Equal(100, allowed.Length);
        Assert.Equal(allowed, CustomerRules.NormalizeEmail(allowed));
        Assert.Throws<InvalidRequestException>(() => CustomerRules.NormalizeEmail("a" + allowed));
    }
}
