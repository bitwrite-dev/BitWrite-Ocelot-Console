using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

public record TokenBlacklistId : ValueObject
{
    public Guid Value { get; init; }

    private TokenBlacklistId(Guid value)
    {
        Value = value;
    }

    public static TokenBlacklistId New() => new(Guid.NewGuid());

    public static TokenBlacklistId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("TokenBlacklistId cannot be empty", "INVALID_TOKEN_BLACKLIST_ID");
        return new TokenBlacklistId(value);
    }

    public static TokenBlacklistId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new DomainException($"Invalid TokenBlacklistId format: {value}", "INVALID_TOKEN_BLACKLIST_ID_FORMAT");
        return From(guid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(TokenBlacklistId id) => id.Value;
    public static implicit operator string(TokenBlacklistId id) => id.Value.ToString();
}
