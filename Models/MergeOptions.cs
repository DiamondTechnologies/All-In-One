namespace All_In_One.Models;

public record CommentTypeOptions(
    bool RemoveSlashSlash = true,
    bool RemoveHash = true,
    bool RemoveTripleQuotes = true,
    bool RemoveSummary = true,
    bool RemoveBlock = true,
    bool RemoveXmlHtml = true
);

public record MergeOptions(
    bool MaskData = true,
    bool WarnKeys = true,
    bool RemoveComments = true,
    CommentTypeOptions? CommentTypes = null
);