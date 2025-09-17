using System.Text.RegularExpressions;
using Utilities;

namespace Presentation;

public static partial class Pres
{

    public static string MakeHTMLAttributeSafe(string input)
    {
        input = FindAmpersand().Replace(input, "&amp;");
        input = ReplaceGreaterThanLessThan(input);
        input = FindDoubleQuote().Replace(input, "&quot;");
        return FindSingleQuote().Replace(input, "&apos;");
    }

    [GeneratedRegex(@"\""")]
    private static partial Regex FindDoubleQuote();

    [GeneratedRegex("'")]
    private static partial Regex FindSingleQuote();

    [GeneratedRegex("&")]
    private static partial Regex FindAmpersand();


    public static string MakeHTMLSafe(string input)
    {
        return ReplaceGreaterThanLessThan(input);
    }


    public static string RemoveNonAlphaNum(string input)
    {
        return FindNonAlphaNum().Replace(input, "");
    }

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex FindNonAlphaNum();


    public static string ReplaceGreaterThanLessThan(string input)
    {
        input = FindLessThan().Replace(input, "&lt;");
        return FindGreaterThan().Replace(input, "&gt;");
    }

    [GeneratedRegex("<")]
    private static partial Regex FindLessThan();

    [GeneratedRegex(">")]
    private static partial Regex FindGreaterThan();


    public static bool ValidateEmail(string emailAddress)
    {
        if (emailAddress.Length > 254) return false;
        return ValidateEmailFormat().IsMatch(emailAddress);
    }

    [GeneratedRegex(@"^\A[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\z$")]
    private static partial Regex ValidateEmailFormat();


    public static string RemoveNonDigits(string input)
    {
        return FindNonDigits().Replace(input, "");
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex FindNonDigits();


    public static string RemoveNonAlpha(string input)
    {
        return FindNonAlpha().Replace(input, "");
    }
    [GeneratedRegex("[^a-zA-Z]")]
    private static partial Regex FindNonAlpha();


    public static UInt128 URLToID(string url)
    {
        url = url[2..] + url[..2];
        url = url.Replace('-', '+').Replace('_', '/');
        byte[] idBytes = Convert.FromBase64String(url + "==");
        (idBytes[0], idBytes[15]) = (idBytes[15], idBytes[0]);
        (idBytes[2], idBytes[13]) = (idBytes[13], idBytes[2]);
        (idBytes[4], idBytes[11]) = (idBytes[11], idBytes[4]);
        (idBytes[6], idBytes[9]) = (idBytes[9], idBytes[6]);
        return Utils.GetU128Val(idBytes);
    }

    public static string IDToURL(UInt128 id)
    {
        byte[] idBytes = Utils.GetU128Bytes(id);
        (idBytes[6], idBytes[9]) = (idBytes[9], idBytes[6]);
        (idBytes[4], idBytes[11]) = (idBytes[11], idBytes[4]);
        (idBytes[2], idBytes[13]) = (idBytes[13], idBytes[2]);
        (idBytes[0], idBytes[15]) = (idBytes[15], idBytes[0]);
        string url = Convert.ToBase64String(idBytes)[..^2].Replace('+', '-').Replace('/', '_');
        url = url[^2..] + url[..^2];
        return url;
    }

    public static bool ValidateIdURL(string url)
    {
        if (url.Length != 22) return false;
        if (FindNonURL().Count(url) != 0) return false;
        return true;
    }

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex FindNonURL();


    public static bool ValidateNonce(string nonce)
    {
        if (nonce.Length != 16) return false;
        if (FindNonHex().Count(nonce) != 0) return false;
        return true;
    }

    [GeneratedRegex("[^a-fA-F0-9]")]
    private static partial Regex FindNonHex();
}