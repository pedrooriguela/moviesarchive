namespace MoviesArchive.Domain.Exceptions;

public enum SubscriptionErrorKind
{
    NotFound,
    Forbidden,
    Conflict,
}

// Regra de assinatura não atendida. Code é fixo (o front decide o que mostrar a partir dele);
// Message é o texto para a pessoa.
public class SubscriptionException : Exception
{
    public SubscriptionException(SubscriptionErrorKind kind, string code, string message) : base(message)
    {
        Kind = kind;
        Code = code;
    }

    public SubscriptionErrorKind Kind { get; }
    public string Code { get; }
}
