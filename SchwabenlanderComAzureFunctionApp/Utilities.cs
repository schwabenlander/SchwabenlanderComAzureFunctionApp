namespace SchwabenlanderComAzureFunctionApp;

public static class Utilities
{
    public static string GetEnvironmentVariable(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Environment variable '{key}' is not set.");
        }
        return value;
    }
}