using DB.DTOs;
using DB.Repository.Utilites;

namespace API.Services.Interfaces;

public interface IBookService
{
    Task<Result<IEnumerable<BookResponse>>> GetBooksAsync(BookFilterRequest request = null);
    Task<Result<BookResponse>> GetBookByIdAsync(Guid bookId);
    Task<Result<BookResponse>> AddBookAsync(Guid ownerId, AddBookRequest request);
    Task<Result<BookResponse>> UpdateBookAsync(Guid bookId, Guid userId, UpdateBookRequest request);
    Task<Result> DeleteBookAsync(Guid bookId, Guid userId);
}
