using BitWrite.OcelotConsole.Domain.Exceptions;

namespace BitWrite.OcelotConsole.Domain.ValueObjects.Identity;

public record SessionId : ValueObject
{
    public Guid Value { get; init; }

    private SessionId(Guid value)
    {
        Value = value;
    }

    public static SessionId New() => new(Guid.NewGuid());

    public static SessionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new DomainException("SessionId cannot be empty", "INVALID_SESSION_ID");
        return new SessionId(value);
    }

    public static SessionId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new DomainException($"Invalid SessionId format: {value}", "INVALID_SESSION_ID_FORMAT");
        return From(guid);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(SessionId id) => id.Value;
    public static implicit operator string(SessionId id) => id.Value.ToString();
}
