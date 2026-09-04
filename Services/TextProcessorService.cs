using System.Text.RegularExpressions;
using All_In_One.Models;

namespace All_In_One.Services;

public class TextProcessorService : ITextProcessorService
{
    private static readonly Regex PrivateKeyRegex = new(@"-----BEGIN (.*)?PRIVATE KEY-----[\s\S]*?-----END \1PRIVATE KEY-----", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(@"(\+?\d{1,3}[\s-]?)?\(?\d{3}\)?[\s-]?\d{3}[\s-]?\d{2}[\s-]?\d{2}", RegexOptions.Compiled);

    public string ProcessText(string content, MergeOptions options, out string? foundKey)
    {
        Match match = PrivateKeyRegex.Match(content);
        bool containsKey = options.WarnKeys && match.Success;

        foundKey = containsKey ? match.Value : null;

        if (options.RemoveComments && options.CommentTypes != null)
        {
            content = RemoveComments(content, options.CommentTypes);
        }

        if (options.MaskData)
        {
            content = MaskSensitiveData(content);
        }

        return content;
    }

    private static string RemoveComments(string content, CommentTypeOptions opts)
    {
        if (opts.RemoveSummary)
        {
            content = Regex.Replace(content, @"///.*$", "", RegexOptions.Multiline);
        }

        if (opts.RemoveBlock)
        {
            content = Regex.Replace(content, @"/\*[\s\S]*?\*/", "");
        }

        if (opts.RemoveTripleQuotes)
        {
            content = Regex.Replace(content, @"''''[\s\S]*?''''", "");
        }

        if (opts.RemoveXmlHtml)
        {
            content = Regex.Replace(content, @"", "");
        }

        if (opts.RemoveSlashSlash)
        {
            content = Regex.Replace(content, @"//.*$", "", RegexOptions.Multiline);
        }

        if (opts.RemoveHash)
        {
            content = Regex.Replace(content, @"#.*$", "", RegexOptions.Multiline);
        }

        return content;
    }

    private static string MaskSensitiveData(string content)
    {
        content = EmailRegex.Replace(content, "***@***.***");
        content = PhoneRegex.Replace(content, "[PHONE REMOVED]");
        return content;
    }
}