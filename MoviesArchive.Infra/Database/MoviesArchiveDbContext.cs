using MoviesArchive.Domain.Models.Users;
using MoviesArchive.Domain.Models.Movies;
using MoviesArchive.Domain.Models.Reviews;
using MoviesArchive.Domain.Models.Lists;
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
    }
}
