namespace MoviesArchive.Domain.Common;

// Quem está fazendo a requisição atual. Um middleware da API preenche logo depois da autenticação,
// e o filtro global de filmes do DbContext usa MaxAgeRating para esconder o que estiver acima da idade.
public class ViewerContext
{
    public Guid? UserId { get; private set; }

    // Maior classificação indicativa que a pessoa pode ver. Nulo = sem restrição: maior de idade,
    // ou idade desconhecida (sem data de nascimento não dá para assinar, então também não dá para assistir).
    public int? MaxAgeRating { get; private set; }

    public void Set(Guid? userId, int? maxAgeRating)
    {
        UserId = userId;
        MaxAgeRating = maxAgeRating;
    }
}
