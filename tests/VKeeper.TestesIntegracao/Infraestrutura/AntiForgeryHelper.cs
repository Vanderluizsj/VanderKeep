using System.Text.RegularExpressions;

namespace VKeeper.TestesIntegracao.Infraestrutura;

public static partial class AntiForgeryHelper
{
    [GeneratedRegex(@"name=""__RequestVerificationToken""[^>]*value=""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    public static string ExtractRequestVerificationToken(string html)
    {
        var match = TokenRegex().Match(html);
        if (!match.Success)
        {
            throw new InvalidOperationException("Anti-forgery token was not found in the HTML response.");
        }

        return match.Groups[1].Value;
    }
}
