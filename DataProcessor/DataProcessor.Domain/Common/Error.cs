namespace DataProcessor.Domain.Common;

public record Error(int Code, string Message)
{
    public static readonly Error None = new(0, string.Empty);
    public static readonly Error NotFound = new(404, "The requested resource was not found.");
    public static readonly Error InternalError = new(500, "An internal error occurred.");

    public static Error Validation(string message) => new(400, message);
}
