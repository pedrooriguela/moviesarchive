namespace MoviesArchive.Domain.Exceptions;

public class VideoUnavailableException : Exception
{
    public VideoUnavailableException() : base("Este filme ainda não tem vídeo no acervo.") { }
}
