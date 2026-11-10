using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Lists;
using MoviesArchive.Domain.Models.Watchlist;
using MoviesArchive.Domain.Models.History;
using MoviesArchive.Domain.Models.Plans;
using MoviesArchive.Domain.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace MoviesArchive.Infra.Database;

public class MoviesArchiveDbContext : DbContext
{
    private readonly ViewerContext _viewer;

    public MoviesArchiveDbContext(
        DbContextOptions<MoviesArchiveDbContext> options,
        ViewerContext viewer
        ) : base(options)
    {
        _viewer = viewer;
    }

    // Lido pelos filtros globais a cada consulta: é a idade de quem fez esta requisição
    private int? ViewerMaxAgeRating => _viewer.MaxAgeRating;

    public DbSet<User> Users => Set<User>();

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<MovieList> Lists => Set<MovieList>();

    public DbSet<ListMovie> ListMovies => Set<ListMovie>();

    public DbSet<WatchlistItem> Watchlist => Set<WatchlistItem>();
    public DbSet<WatchHistoryEntry> WatchHistory => Set<WatchHistoryEntry>();

    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<FamilyInvite> FamilyInvites => Set<FamilyInvite>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Review>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(review => review.UserId);

        modelBuilder.Entity<Review>()
            .HasOne<Movie>()
            .WithMany()
            .HasForeignKey(review => review.MovieId);

        //retirar se um usuario puder avaliar o mesmo filme mais de uma vez
        modelBuilder.Entity<Review>()
            .HasIndex(review => new { review.UserId, review.MovieId })
            .IsUnique();

        // Nota de 0,5 a 5 em passos de meia estrela (ver database/2026-11-03_avaliacoes_e_watchlist.sql)
        modelBuilder.Entity<Review>()
            .Property(review => review.Rating)
            .HasColumnType("numeric(2,1)");

        modelBuilder.Entity<WatchlistItem>()
            .ToTable("Watchlist")
            .HasKey(item => new { item.UserId, item.MovieId });

        modelBuilder.Entity<WatchlistItem>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(item => item.UserId);

        modelBuilder.Entity<WatchlistItem>()
            .HasOne<Movie>()
            .WithMany()
            .HasForeignKey(item => item.MovieId);

        modelBuilder.Entity<MovieList>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(list => list.UserId);

        modelBuilder.Entity<MovieList>()
            .HasMany(list => list.Items)
            .WithOne()
            .HasForeignKey(item => item.ListId);

        modelBuilder.Entity<MovieList>()
            .HasIndex(list => new { list.UserId, list.Title })
            .IsUnique();

        modelBuilder.Entity<ListMovie>()
            .HasKey(item => new { item.ListId, item.MovieId });

        modelBuilder.Entity<ListMovie>()
            .HasOne(item => item.Movie)
            .WithMany()
            .HasForeignKey(item => item.MovieId);

        modelBuilder.Entity<WatchHistoryEntry>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(entry => entry.UserId);

        modelBuilder.Entity<WatchHistoryEntry>()
            .HasOne(entry => entry.Movie)
            .WithMany()
            .HasForeignKey(entry => entry.MovieId);

        modelBuilder.Entity<WatchHistoryEntry>()
            .HasIndex(entry => new { entry.UserId, entry.WatchedAt });

        modelBuilder.Entity<Movie>()
            .Property(movie => movie.Genres)
            .HasColumnType("text[]");

        // translate() do PostgreSQL, usada pela busca para ignorar acentos (ver SearchText)
        modelBuilder.HasDbFunction(typeof(SearchText).GetMethod(nameof(SearchText.Translate))!)
            .HasName("translate")
            .IsBuiltIn();

        // ---- Classificação indicativa: menores não veem filmes acima da própria idade em
        // nenhuma consulta (busca, prateleiras, ficha, avaliações, watchlist, listas, histórico, play).
        // Um filme escondido se comporta como inexistente.
        modelBuilder.Entity<Movie>()
            .HasQueryFilter(movie => ViewerMaxAgeRating == null || movie.AgeRating <= ViewerMaxAgeRating);

        // Itens que apontam para um filme escondido também somem, e as contagens batem
        modelBuilder.Entity<ListMovie>()
            .HasQueryFilter(item => ViewerMaxAgeRating == null || item.Movie!.AgeRating <= ViewerMaxAgeRating);

        modelBuilder.Entity<WatchHistoryEntry>()
            .HasQueryFilter(entry => ViewerMaxAgeRating == null || entry.Movie!.AgeRating <= ViewerMaxAgeRating);

        // ---- Planos e assinaturas (ver database/2026-11-07_assinaturas.sql)
        modelBuilder.Entity<Plan>(plan =>
        {
            plan.ToTable("Plans");
            plan.Property(item => item.Type).HasConversion<string>();
            plan.Property(item => item.Price).HasColumnType("numeric(10,2)");
            plan.HasIndex(item => item.Name).IsUnique();
        });

        modelBuilder.Entity<Subscription>(subscription =>
        {
            subscription.ToTable("Subscriptions");
            subscription.Property(item => item.EndReason).HasConversion<string>();

            subscription.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.OwnerId);

            // Plano com assinaturas não pode ser apagado (o admin o tira de venda)
            subscription.HasOne<Plan>()
                .WithMany()
                .HasForeignKey(item => item.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Integrantes: cada usuário aponta para a assinatura ativa de que faz parte.
        // Junto com Subscriptions.OwnerId, as duas tabelas apontam uma para a outra.
        modelBuilder.Entity<User>()
            .HasOne<Subscription>()
            .WithMany()
            .HasForeignKey(user => user.SubscriptionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<FamilyInvite>(invite =>
        {
            invite.ToTable("FamilyInvites");
            invite.Property(item => item.Status).HasConversion<string>();

            invite.HasOne<Subscription>()
                .WithMany()
                .HasForeignKey(item => item.SubscriptionId);

            invite.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.InvitedUserId);
        });

    }
}
