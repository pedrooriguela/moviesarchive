namespace MoviesArchive.Domain.Exceptions;

public class NotificationNotFoundException : Exception
{
    public NotificationNotFoundException() : base("Notificação não encontrada.") { }
}
