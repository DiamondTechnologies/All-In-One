using All_In_One.Models;

namespace All_In_One.Services;

public interface ITextProcessorService
{
    string ProcessText(string content, MergeOptions options, out string? foundKey);
}