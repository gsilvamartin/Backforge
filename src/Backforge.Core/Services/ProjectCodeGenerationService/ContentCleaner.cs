namespace Backforge.Core.Services.ProjectCodeGenerationService;

/// <summary>
/// Static class for handling content cleaning
/// </summary>
public static class ContentCleaner
{
    /// <summary>
    /// Cleans generated content by removing code block markers and other artifacts
    /// </summary>
    public static string CleanGeneratedContent(string content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Remove code block markers if present
        content = content.Trim();
        if (content.StartsWith("```") && content.EndsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline != -1)
            {
                content = content.Substring(firstNewline + 1, content.Length - firstNewline - 4).Trim();
            }
            else
            {
                content = string.Empty;
            }
        }
        else if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline != -1)
            {
                content = content.Substring(firstNewline + 1).Trim();
            }
        }
        else if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3).Trim();
        }

        return content;
    }
}

