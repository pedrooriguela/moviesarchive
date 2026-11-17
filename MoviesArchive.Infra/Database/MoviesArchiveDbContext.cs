using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Lists;
using MoviesArchive.Domain.Models.History;
using MoviesArchive.Domain.Models.Plans;
using MoviesArchive.Domain.Models.Subscriptions;
using MoviesArchive.Domain.Models.Auth;
using MoviesArchive.Domain.Models.Notifications;
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

    // Respostas às avaliações (tabela "Responses") e curtidas
    public DbSet<ReviewReply> ReviewReplies => Set<ReviewReply>();
    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();
    public DbSet<ReviewReplyLike> ReviewReplyLikes => Set<ReviewReplyLike>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<MovieList> Lists => Set<MovieList>();

    public DbSet<ListMovie> ListMovies => Set<ListMovie>();

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

        // Uma watchlist por usuário (ver database/2026-11-17_admin_e_watchlist_nas_listas.sql)
        modelBuilder.Entity<MovieList>()
            .HasIndex(list => list.UserId)
            .IsUnique()
            .HasFilter("\"IsWatchlist\"");

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

        // ---- Respostas e curtidas (ver database/2026-11-17_respostas_curtidas_tokens_video.sql)
        modelBuilder.Entity<ReviewReply>(reply =>
        {
            reply.ToTable("Responses");
            reply.Property(item => item.ParentReplyId).HasColumnName("ParentResponseId");
            reply.Ignore(item => item.IsDeleted);

            reply.HasOne<Review>()
                .WithMany()
                .HasForeignKey(item => item.ReviewId);

            reply.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.UserId);

            reply.HasOne<ReviewReply>()
                .WithMany()
                .HasForeignKey(item => item.ParentReplyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ReviewLike>(like =>
        {
            like.ToTable("ReviewLikes");
            like.HasKey(item => new { item.UserId, item.ReviewId });
        });

        modelBuilder.Entity<ReviewReplyLike>(like =>
        {
            like.ToTable("ResponseLikes");
            like.HasKey(item => new { item.UserId, item.ReplyId });
            like.Property(item => item.ReplyId).HasColumnName("ResponseId");
        });

        // ---- Notificações de curtidas e respostas (ver database/2026-11-17_notificacoes.sql)
        modelBuilder.Entity<Notification>(notification =>
        {
            notification.ToTable("Notifications");
            notification.Property(item => item.Type).HasConversion<string>();
            notification.Property(item => item.ReplyId).HasColumnName("ResponseId");
            notification.HasIndex(item => new { item.UserId, item.CreatedAt });
        });

        // ---- Sessões: refresh tokens guardados como hash
        modelBuilder.Entity<RefreshToken>(token =>
        {
            token.ToTable("RefreshTokens");
            token.HasIndex(item => item.TokenHash).IsUnique();
            token.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.UserId);
        });

        // translate() do PostgreSQL, usada pela busca para ignorar acentos (ver SearchText)
        modelBuilder.HasDbFunction(typeof(SearchText).GetMethod(nameof(SearchText.Translate))!)
            .HasName("translate")
            .IsBuiltIn();

        // ---- Classificação indicativa: menores não veem filmes acima da própria idade em
        // nenhuma consulta (busca, prateleiras, ficha, avaliações, watchlist, listas, histórico, play).
        // Um filme escondido se comporta como inexistente.
        // ---- Filmes removidos pelo administrador também somem de todas as consultas, menos do
        // histórico (WatchHistoryRepository desliga só o filtro Removed) e do painel do administrador.
        modelBuilder.Entity<Movie>()
            .HasQueryFilter(MovieQueryFilters.AgeRating,
                movie => ViewerMaxAgeRating == null || movie.AgeRating <= ViewerMaxAgeRating)
            .HasQueryFilter(MovieQueryFilters.Removed, movie => movie.DeletedAt == null);

        // Itens que apontam para um filme escondido ou removido também somem, e as contagens batem.
        // A linha continua na lista: se o administrador restaurar o filme, ele volta a aparecer.
        modelBuilder.Entity<ListMovie>()
            .HasQueryFilter(item => item.Movie!.DeletedAt == null &&
                (ViewerMaxAgeRating == null || item.Movie!.AgeRating <= ViewerMaxAgeRating));

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
