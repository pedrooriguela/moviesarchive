using System.ComponentModel.DataAnnotations;

namespace MoviesArchive.Domain.Models.Admin.Dtos;

// Cadastro e edição de filme pelo administrador. As regras que dependem do domínio
// (gêneros, classificação indicativa, endereços) são conferidas no MovieAdminService.
public class SaveMovieRequestDto
{
    [Required(ErrorMessage = "O título é obrigatório")]
    [StringLength(200, ErrorMessage = "O título deve ter até 200 caracteres")]
    public string Title { get; set; } = string.Empty;

    public DateOnly? ReleaseDate { get; set; }

    [StringLength(4000, ErrorMessage = "A sinopse deve ter até 4000 caracteres")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "O idioma deve ter até 50 caracteres")]
    public string? Language { get; set; }

    [StringLength(200, ErrorMessage = "A direção deve ter até 200 caracteres")]
    public string? Director { get; set; }

    [StringLength(2000, ErrorMessage = "O endereço do pôster deve ter até 2000 caracteres")]
    public string? PosterUrl { get; set; }

    // Códigos do enum Genre (ex.: "Acao", "FiccaoCientifica"); pelo menos um
    public List<string> Genres { get; set; } = [];

    // Idade mínima: 0 (livre), 10, 12, 14, 16 ou 18
    public int? AgeRating { get; set; }

    public int? RuntimeMinutes { get; set; }

    [StringLength(2000, ErrorMessage = "O endereço do vídeo deve ter até 2000 caracteres")]
    public string? VideoUrl { get; set; }
}
