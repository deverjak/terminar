namespace Terminar.SharedKernel.ValueObjects;

public sealed record TenantId(Guid Value)
{
    public static TenantId New() => new(Guid.NewGuid());

    public static TenantId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("TenantId cannot be empty.", nameof(value));
        return new TenantId(value);
    }

    public override string ToString() => Value.ToString();
}
