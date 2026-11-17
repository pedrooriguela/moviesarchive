namespace MoviesArchive.Domain.Exceptions;

// Garante que sempre sobra pelo menos um administrador: quem tira o acesso de outro continua admin
public class AdminSelfDemotionException : Exception
{
    public AdminSelfDemotionException() : base("Você não pode tirar o seu próprio acesso de administrador") { }
}
