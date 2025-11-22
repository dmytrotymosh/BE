using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;

namespace API.Services.Realisations;

public class GenreService(IGenericRepository<Book> bookRepository) : IGenreService
{
    public async Task<Result<IEnumerable<GenreResponse>>> GetGenresAsync()
    {
        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: null,
            includes: null,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<IEnumerable<GenreResponse>>.Fail(booksResult.Error);
        }

        var uniqueGenres = booksResult.Data
            .Select(book => book.Genre)
            .Where(genre => !string.IsNullOrWhiteSpace(genre))
            .Distinct()
            .OrderBy(genre => genre)
            .Select(genre => new GenreResponse { Name = genre });

        return Result<IEnumerable<GenreResponse>>.Ok(uniqueGenres);
    }
}
