using System.Text.RegularExpressions;

namespace Dsposal.Flow.Extensions;

public static class PatternExtractorExtensions
{
    private static readonly Regex[] PermitPatterns =
    [
        // EPA / NRW
        new Regex(@"EPR/[A-Z]{2}\d{4}[A-Z]{2}(?:/(D|A|V|T|S)\d{4})?", RegexOptions.IgnoreCase),
        new Regex(@"[A-Z]{2}\d{4}[A-Z]{2}(?:/(D|A|V|T|S)\d{4})?", RegexOptions.IgnoreCase),
        new Regex(@"EAWML\d{6}", RegexOptions.IgnoreCase),
        new Regex(@"WML\d{6}", RegexOptions.IgnoreCase),
        // SEPA
        new Regex(@"PPC/[AWNE]/(?:\d{7}|SEPA\d{4}-\d{4})", RegexOptions.IgnoreCase),
        new Regex(@"WML/[LWEN]/(?:\d{7}|SEPA\d{4}-\d{4})", RegexOptions.IgnoreCase),
        new Regex(@"EAS/P/\d{6}", RegexOptions.IgnoreCase),
        // NI
        new Regex(@"P\d{4}/\d{2}[A-Z](?:/V\d+)?", RegexOptions.IgnoreCase),
        new Regex(@"WPPC \d{2}/\d{2}(?:/V\d+)?", RegexOptions.IgnoreCase),
        new Regex(@"PAC/\d{4}/WCL\d{3}", RegexOptions.IgnoreCase),
        new Regex(@"WML \d{2}/\d+(?:/T)?", RegexOptions.IgnoreCase),
        new Regex(@"LN/\d{2}/(?:0?[1-9]\d*)(?:/(?:M|T|C|V\d+))*", RegexOptions.IgnoreCase),
    ];

    private static readonly Regex[] RegistrationPatterns =
    [
        // EPA / NRW
        new Regex(@"CBD[LU]\d{3,}", RegexOptions.IgnoreCase),
        // SEPA
        new Regex(@"WCR/R/\d{7}", RegexOptions.IgnoreCase),
        new Regex(@"SCO/\d{7}", RegexOptions.IgnoreCase),
        new Regex(@"SEA/\d{7}", RegexOptions.IgnoreCase),
        new Regex(@"SNO/\d{7}", RegexOptions.IgnoreCase),
        new Regex(@"SWE/\d{7}", RegexOptions.IgnoreCase),
        new Regex(@"WCR/\d{7}", RegexOptions.IgnoreCase),

        // NI 
        new Regex(@"ROC\s?(?:UT|LT)\s?\d{1,5}", RegexOptions.IgnoreCase),
    ];

    public static IEnumerable<string> ExtractPermits(this string input)
    {
        foreach (var pattern in PermitPatterns)
        {
            foreach (Match match in pattern.Matches(input))
            {
                yield return match.Value;
            }
        }
    }
    
    public static IEnumerable<string> ExtractRegistrations(this string input)
    {
        foreach (var pattern in RegistrationPatterns)
        {
            foreach (Match match in pattern.Matches(input))
            {
                yield return match.Value;
            }
        }
    }
    
}