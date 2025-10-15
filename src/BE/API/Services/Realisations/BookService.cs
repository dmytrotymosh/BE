using API.Services.Interfaces;
using BookLoop.Data.Models;
using DB.DTOs;
using DB.Models;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Realisations;

public class BookService(IGenericRepository<Book> bookRepository) : IBookService
{
    public async Task<Result<IEnumerable<BookResponse>>> GetBooksAsync(Guid? ownerId = null)
    {
        var includes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };
        var booksResult = await bookRepository.GetListAsync<Book>(
            filter: ownerId.HasValue ? b => b.OwnerId == ownerId.Value : null,
            includes: includes,
            selector: null
        );

        if (!booksResult.Success)
        {
            return Result<IEnumerable<BookResponse>>.Fail(booksResult.Error);
        }
        var bookResponses = booksResult.Data.Select(book => new BookResponse
        {
            Id = book.Id,
            OwnerId = book.OwnerId,
            OwnerFirstName = book.Owner?.FirstName ?? string.Empty,
            OwnerLastName = book.Owner?.LastName ?? string.Empty,
            Title = book.Title,
            Description = book.Description,
            State = book.State,
            Genre = book.Genre,
            Created = book.Created,
            Modified = book.Modified
        });

        return Result<IEnumerable<BookResponse>>.Ok(bookResponses);
    }

    public async Task<Result<BookResponse>> GetBookByIdAsync(Guid bookId)
    {
        var includes = new List<Func<IQueryable<Book>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Book, object>>>
        {
            query => query.Include(b => b.Owner)
        };
        var bookResult = await bookRepository.GetSingleAsync<Book>(
            filter: b => b.Id == bookId,
            includes: includes,
            selector: null
        );

        if (!bookResult.Success || bookResult.Data == null)
        {
            return Result<BookResponse>.Fail("Book not found");
        }
        var book = bookResult.Data;
        var bookResponse = new BookResponse
        {
            Id = book.Id,
            OwnerId = book.OwnerId,
            OwnerFirstName = book.Owner?.FirstName ?? string.Empty,
            OwnerLastName = book.Owner?.LastName ?? string.Empty,
            Title = book.Title,
            Description = book.Description,
            State = book.State,
            Genre = book.Genre,
            Created = book.Created,
            Modified = book.Modified
        };

        return Result<BookResponse>.Ok(bookResponse);
    }

    public async Task<Result<BookResponse>> AddBookAsync(Guid ownerId, AddBookRequest request)
    {
        var book = new Book
        {
            OwnerId = ownerId,
            Title = request.Title,
            Description = request.Description,
            State = request.State,
            Genre = request.Genre
        };

        var addResult = await bookRepository.AddAsync(book);

        if (!addResult.Success)
        {
            return Result<BookResponse>.Fail(addResult.Error);
        }
        
        return await GetBookByIdAsync(book.Id);
    }
}
