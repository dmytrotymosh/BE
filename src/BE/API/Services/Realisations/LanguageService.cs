using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;

namespace API.Services.Realisations;

public class LanguageService(IGenericRepository<Language> languageRepository) : ILanguageService
{
    public async Task<Result<IEnumerable<LanguageResponse>>> GetLanguagesAsync()
    {
        var languagesResult = await languageRepository.GetListAsync<Language>(
            filter: null,
            includes: null,
            selector: null
        );

        if (!languagesResult.Success)
        {
            return Result<IEnumerable<LanguageResponse>>.Fail(languagesResult.Error);
        }

        var languageResponses = languagesResult.Data.Select(language => new LanguageResponse
        {
            Id = language.Id,
            Name = language.Name,
            Code = language.Code
        });

        return Result<IEnumerable<LanguageResponse>>.Ok(languageResponses);
    }
}
