using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IBookService
{
    Task<Result<IEnumerable<BookResponse>>> GetBooksAsync(Guid? ownerId = null);
    Task<Result<BookResponse>> GetBookByIdAsync(Guid bookId);
    Task<Result<BookResponse>> AddBookAsync(Guid ownerId, AddBookRequest request);
}
