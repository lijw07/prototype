namespace Prototype.Utility;

public static class ErrorLogger
{
    public static void Log(Exception ex, string? contextInfo = null)
    {
        var context = string.IsNullOrWhiteSpace(contextInfo) ? "Unknown Context" : contextInfo;
        switch (ex)
        {
            case System.Text.Json.JsonException:
                Console.WriteLine($"JSON parsing failed for {context}: {ex.Message}");
                break;
            case IOException:
                Console.WriteLine($"File error for {context}: {ex.Message}");
                break;
            case ArgumentException:
                Console.WriteLine($"Duplicate name detected for {context}: {ex.Message}");
                break;
            default:
                Console.WriteLine($"Error processing {context}: {ex.Message}");
                break;
        }
    }
}