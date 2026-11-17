namespace MoviesArchive.Domain.Exceptions;

// Campo do cadastro de filme com valor inválido; Field é o nome do campo no formulário
public class InvalidMovieDataException : Exception
{
    public InvalidMovieDataException(string field, string message) : base(message)
    {
        Field = field;
    }

    public string Field { get; }
}
