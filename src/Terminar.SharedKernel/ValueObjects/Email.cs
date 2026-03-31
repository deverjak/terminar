using System.Text.RegularExpressions;

namespace Terminar.SharedKernel.ValueObjects;

public sealed partial record Email
{
    private static readonly Regex EmailRegex = CreateEmailRegex();

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid email address.", nameof(value));

        return new Email(normalized);
    }

    public override string ToString() => Value;
    [GeneratedRegex(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "cs-CZ")]
    private static partial Regex CreateEmailRegex();

}
