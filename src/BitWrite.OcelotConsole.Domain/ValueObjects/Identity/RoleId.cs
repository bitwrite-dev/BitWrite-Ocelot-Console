using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

public record RoleId : ValueObject
{
    public string Value { get; init; }

    private RoleId(string value)
    {
        Value = value;
    }

    public static RoleId New() => new(Guid.NewGuid().ToString());

    public static RoleId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("RoleId cannot be empty", "INVALID_ROLE_ID");

        return new RoleId(value.Trim().ToLowerInvariant());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(RoleId id) => id.Value;
}
