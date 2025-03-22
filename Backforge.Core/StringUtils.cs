namespace Backforge.Core;

public static class StringUtils
{
    public static string TruncateString(string input, int maxLength)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
        {
            return input;
        }

        return string.Concat(input.AsSpan(0, maxLength), "...");
    }
}

public static class SystemUtils
{
    public static bool DetectGpu()
    {
        try
        {
            var hasEnvFlag = !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("GPU_AVAILABLE"));

            return hasEnvFlag;
        }
        catch
        {
            return false;
        }
    }
}