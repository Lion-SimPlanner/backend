namespace LionSimPlanner.Scheduling.Domain.Validation;

/// <summary>
/// Result returned by the FTL Validation Service.
/// If IsValid is false, Violations contains exactly one or more human-readable messages
/// that the API layer surfaces directly to the Training Admin — never a generic error.
///
/// The design principle: each violation message must be specific enough that the Admin
/// knows exactly who violated what rule and by how much, without having to look up
/// anything else.
/// </summary>
public sealed class FtlValidationResult
{
    public bool IsValid => Violations.Count == 0;
    public List<string> Violations { get; } = [];

    public void AddViolation(string message) => Violations.Add(message);

    /// <summary>
    /// Convenience factory for a successful (no violations) result.
    /// </summary>
    public static FtlValidationResult Success() => new();

    /// <summary>Returns a single-violation failure.</summary>
    public static FtlValidationResult Fail(string message)
    {
        var result = new FtlValidationResult();
        result.AddViolation(message);
        return result;
    }
}
