namespace SchwabenlanderComAzureFunctionApp;

public record ContactFormMessage(string Name, string Email, string Phone, string Message)
{
    // ReSharper disable once InconsistentNaming
    public Guid id { get; set; }

    public DateTimeOffset MessageTimeStamp { get; set; }
}