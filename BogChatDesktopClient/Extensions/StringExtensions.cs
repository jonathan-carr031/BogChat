using System.Text.RegularExpressions;

namespace BogChatDesktopClient.Extensions;

public static partial class StringExtensions {
    private static readonly Regex RemoveNonStandardRegex = CleanStringRegex();

    public static string RemoveNonStandardCharacters(this string str) {
        return RemoveNonStandardRegex.Replace(str, "");
    }

    [GeneratedRegex(@"[^A-Za-z0-9'\s]+")]
    private static partial Regex CleanStringRegex();
}