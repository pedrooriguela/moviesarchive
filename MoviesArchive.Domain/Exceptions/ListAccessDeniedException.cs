namespace MoviesArchive.Domain.Exceptions;

public class ListAccessDeniedException : Exception
{
    public ListAccessDeniedException() : base("Você não tem permissão para alterar esta lista") { }
}
