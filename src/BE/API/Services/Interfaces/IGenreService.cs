using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IGenreService
{
    Task<Result<IEnumerable<GenreResponse>>> GetGenresAsync();
}
