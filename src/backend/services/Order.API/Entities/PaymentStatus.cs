namespace Order.API.Entities;

public static class PaymentStatus
{
    public const string PENDING = "PENDING";
    public const string SUCCESS = "SUCCESS";
    public const string FAILED = "FAILED";

    public static readonly string[] ValidStatuses = [PENDING, SUCCESS, FAILED];

    public static bool IsValid(string status) =>
        ValidStatuses.Contains(status.ToUpperInvariant());
}
