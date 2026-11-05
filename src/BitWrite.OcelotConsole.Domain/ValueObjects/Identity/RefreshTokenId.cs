using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

public record RefreshTokenId : ValueObject
{
    public Guid Value { get; init; }

    private RefreshTokenId(Guid value)
    {
        Value = value;
    }

    public static RefreshTokenId New() => new(Guid.NewGuid());

    public static RefreshTokenId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("RefreshTokenId cannot be empty", "INVALID_REFRESH_TOKEN_ID");
        return new RefreshTokenId(value);
    }

    public static RefreshTokenId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new DomainException($"Invalid RefreshTokenId format: {value}", "INVALID_REFRESH_TOKEN_ID_FORMAT");
        return From(guid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(RefreshTokenId id) => id.Value;
    public static implicit operator string(RefreshTokenId id) => id.Value.ToString();
}
