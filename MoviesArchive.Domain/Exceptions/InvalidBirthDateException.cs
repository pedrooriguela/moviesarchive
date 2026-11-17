namespace MoviesArchive.Domain.Exceptions;

public class InvalidBirthDateException : Exception
{
    public InvalidBirthDateException() : base("Informe uma data de nascimento válida.") { }
}
