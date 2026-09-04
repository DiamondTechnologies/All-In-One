using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using All_In_One.Models;

namespace All_In_One.Services;

public class FileService(ITextProcessorService textProcessor) : IFileService
{
    private readonly ITextProcessorService _textProcessor = textProcessor;

    private static readonly HashSet<string> BinaryExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".so", ".dylib", ".bin", ".dat", ".db", ".sqlite",
        ".png", ".jpg", ".jpeg", ".gif", ".ico", ".bmp", ".webp",
        ".zip", ".tar", ".gz", ".7z", ".rar",
        ".pdf", ".docx", ".xlsx", ".pptx", ".mp3", ".mp4", ".avi"
    };

    private static bool IsTextFile(string path)
    {
        string ext = Path.GetExtension(path);
        return !BinaryExtensions.Contains(ext);
    }

    public async Task<(List<string> ScannedFiles, bool AccessDenied)> ScanPathsAsync(IEnumerable<string> paths)
    {
        bool accessDenied = false;

        List<string> scanned = await Task.Run(() =>
        {
            var result = new List<string>();
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        IEnumerable<string> files = Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                                             .Where(f => IsTextFile(f));
                        result.AddRange(files);
                    }
                    catch (UnauthorizedAccessException) { accessDenied = true; }
                    catch (DirectoryNotFoundException) { }
                }
                else if (File.Exists(path))
                {
                    try
                    {
                        if (IsTextFile(path))
                        {
                            result.Add(path);
                        }
                    }
                    catch (UnauthorizedAccessException) { accessDenied = true; }
                }
            }
            return result;
        });

        return (scanned, accessDenied);
    }

    public async Task<Dictionary<string, string>> MergeFilesAsync(List<string> selectedFiles, string outputPath, MergeOptions options, IProgress<int> progress)
    {
        var filesWithKeys = new Dictionary<string, string>();

        await Task.Run(() =>
        {
            var sb = new StringBuilder();
            int total = selectedFiles.Count;

            for (int i = 0; i < total; i++)
            {
                string file = selectedFiles[i];
                if (File.Exists(file))
                {
                    string content = File.ReadAllText(file, Encoding.UTF8);
                    content = _textProcessor.ProcessText(content, options, out string? foundKey);

                    if (!string.IsNullOrEmpty(foundKey))
                    {
                        filesWithKeys[Path.GetFileName(file)] = foundKey;
                    }

                    _ = sb.AppendLine($"--- {Path.GetFileName(file)} ---");
                    _ = sb.AppendLine(content);
                    _ = sb.AppendLine();
                }

                int currentPercent = (int) ((double) (i + 1) / total * 100);
                progress.Report(currentPercent);
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        });

        return filesWithKeys;
    }
}