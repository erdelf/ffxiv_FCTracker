namespace FCTracker;

using System.Text.RegularExpressions;

internal static partial class RegexHelper
{
    [GeneratedRegex(@"""([^""]+)""|\S+", RegexOptions.CultureInvariant)]
    public static partial Regex ArgumentParserRegex();
}
