using MoviesArchive.Infra.Database;
using MoviesArchive.Infra.Repositories;
using Microsoft.EntityFrameworkCore;
using MoviesArchive.Domain.Services;
using MoviesArchive.Domain.Interfaces;
using MoviesArchive.Infra.Services;
using MoviesArchive.Infra.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using MoviesArchive.Domain.Common;
using MoviesArchive.Domain.Models.Users;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
var postgresSettings = builder.Configuration.GetSection("PostgresSettings").Get<PostgresSettings>()!;

builder.Services.AddDbContext<MoviesArchiveDbContext>(options =>
    options.UseNpgsql(postgresSettings.ConnectionString));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
        };
    });


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserLoginService, UserLoginService>();
builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IAccountDeletionService, AccountDeletionService>();
builder.Services.AddScoped<IMovieListRepository, MovieListRepository>();
builder.Services.AddScoped<IMovieListService, MovieListService>();
builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieCatalogService, MovieCatalogService>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IWatchlistRepository, WatchlistRepository>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IWatchHistoryRepository, WatchHistoryRepository>();
builder.Services.AddScoped<IWatchHistoryService, WatchHistoryService>();
builder.Services.AddScoped<ViewerContext>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();          

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                   
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "MoviesArchive API v1");
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

// Idade de quem está pedindo: os filtros globais de filmes escondem o que estiver acima dela.
// Lê do banco a cada requisição (e não do token) para valer já no dia do aniversário.
app.Use(async (context, next) =>
{
    var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
    if (Guid.TryParse(value, out var userId))
    {
        var user = await context.RequestServices.GetRequiredService<IUserRepository>().GetByIdAsync(userId);
        context.RequestServices.GetRequiredService<ViewerContext>()
            .Set(userId, user?.MaxAgeRatingOn(BirthDates.Today()));
    }

    await next();
});
app.MapControllers();

app.Run();