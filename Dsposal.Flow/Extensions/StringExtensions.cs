using System.Text.RegularExpressions;

namespace Dsposal.Flow.Extensions;

public static class StringExtensions
{
    public static string? CleanupPostcode(this string? postcode)
    {
        if (string.IsNullOrWhiteSpace(postcode)) return null;
        
        if(postcode.Equals("N/A", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("NA", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Not entered", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Nothing entered", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Not applicable", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Not applicable (N/A)", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Not specified", StringComparison.InvariantCultureIgnoreCase)
           || postcode.Equals("Not provided", StringComparison.InvariantCultureIgnoreCase))
            return null;
        
        var pc = Regex.Replace(postcode, "\\s", string.Empty).ToUpper();

        // Only reformat if it is alphanumeric
        var regEx = new Regex("^[a-zA-Z0-9]*$");
        
        if (!regEx.IsMatch(pc)) return postcode;

        int length = pc.Length, rightStart = length - 3;

        if (length > 7 || length < 5) return postcode; // Invalid postcode, we can't reformat!
        return pc.Insert(rightStart, "\u0020"); // Insert space before last three characters.
    }
}