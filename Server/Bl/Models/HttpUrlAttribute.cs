using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Bl.Models
{
    /// <summary>
    /// An optional address of a picture. Empty means "no picture"; anything else must be a normal
    /// http or https web address with a host name. Other schemes (ftp, javascript, data, file...) are refused,
    /// because the address ends up in an image tag in the browser.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public sealed class HttpUrlAttribute : ValidationAttribute
    {
        private static readonly Regex Shape = new(@"^https?://[^/\s?#]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public HttpUrlAttribute() : base("The {0} field must be a web address starting with http:// or https://.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is null) return true;
            if (value is not string text) return false;
            text = text.Trim();
            if (text.Length == 0) return true;

            return Shape.IsMatch(text)
                   && Uri.TryCreate(text, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                   && !string.IsNullOrEmpty(uri.Host);
        }
    }
}
