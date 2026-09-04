using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using All_In_One.Models;

namespace All_In_One.Services;

public interface IFileService
{
    Task<(List<string> ScannedFiles, bool AccessDenied)> ScanPathsAsync(IEnumerable<string> paths);
    Task<Dictionary<string, string>> MergeFilesAsync(List<string> selectedFiles, string outputPath, MergeOptions options, IProgress<int> progress);
}