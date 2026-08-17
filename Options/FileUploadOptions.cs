namespace MIC.risk.Options;

public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public long MaxFileSizeBytes { get; set; } = 40 * 1024 * 1024;

    public string UploadSubdirectory { get; set; } = "uploads";

    public Dictionary<string, string[]> AllowedExtensionsByType { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Image"] = [".png", ".jpg", ".jpeg", ".gif", ".webp"],
        ["File"] = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".mp4", ".mp3", ".av1", ".m4a"]
    };
}
