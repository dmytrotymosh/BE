using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface ILanguageService
{
    Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync();
}
