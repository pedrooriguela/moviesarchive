using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Lists;
using MoviesArchive.Domain.Models.Watchlist;
using Microsoft.EntityFrameworkCore;

namespace MoviesArchive.Infra.Database;

public class MoviesArchiveDbContext : DbContext
{
    public MoviesArchiveDbContext(
        DbContextOptions<MoviesArchiveDbContext> options
        ) : base(options) { }

    public DbSet<User> Users => Set<User>();

    public DbSet<Movie> Movies => Set<Movie>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<MovieList> Lists => Set<MovieList>();

    public DbSet<ListMovie> ListMovies => Set<ListMovie>();

    public DbSet<WatchlistItem> Watchlist => Set<WatchlistItem>();

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

        modelBuilder.Entity<Movie>()
            .Property(movie => movie.Genres)
            .HasColumnType("text[]");

    }
}
