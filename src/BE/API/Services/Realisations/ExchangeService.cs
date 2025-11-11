using API.Services.Interfaces;
using DB.DTOs;
using DB.Models;
using API.Services.Realisations.Utilites;
using DB.Models.Enums;
using DB.Repository;
using DB.Repository.Utilites;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace API.Services.Realisations;

public class ExchangeService(
    IGenericRepository<Exchange> exchangeRepository,
    INotificationService notificationService) 
    : IExchangeService
{
    public async Task<Result<IEnumerable<ExchangeResponse>>> GetExchangesAsync(ExchangeFilterRequest request = null)
    {
        var includes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query
                .Include(e => e.Book)
                .Include(e => e.Owner)
                .Include(e => e.Receiver)
        };

        Expression<Func<Exchange, bool>> filter = e => true;

        if (request is not null)
        {
            if (request.UserId.HasValue)
                filter = filter.And(e => e.OwnerId == request.UserId.Value || e.ReceiverId == request.UserId.Value);
            else if (request.ReceiverId.HasValue)
                filter = filter.And(e => e.ReceiverId == request.ReceiverId.Value);
            else if (request.OwnerId.HasValue)
                filter = filter.And(e => e.OwnerId == request.OwnerId.Value);
            if (request.BookId.HasValue)
                filter = filter.And(e => e.BookId == request.BookId.Value);
            if (request.status.HasValue)
                filter = filter.And(e => e.Status == request.status.Value);
            if (request.CreatedFrom.HasValue)
                filter = filter.And(e => e.Created >= request.CreatedFrom.Value);
            if (request.CreatedTo.HasValue)
                filter = filter.And(e => e.Created <= request.CreatedTo.Value);
        }

        var exchangesResult = await exchangeRepository.GetListAsync<Exchange>(
            filter: filter,
            includes: includes,
            selector: null
        );

        if (!exchangesResult.Success)
        {
            return Result<IEnumerable<ExchangeResponse>>.Fail(exchangesResult.Error);
        }
        var exchangeResponses = exchangesResult.Data.Select(exchange => new ExchangeResponse
        {
            Id = exchange.Id,
            BookId = exchange.Book.Id,
            BookTitle = exchange.Book.Title,
            OwnerId = exchange.OwnerId,
            OwnerFirstName = exchange.Owner?.FirstName ?? string.Empty,
            OwnerLastName = exchange.Owner?.LastName ?? string.Empty,
            ReceiverId = exchange.ReceiverId,
            ReceiverFirstName = exchange.Receiver?.FirstName ?? string.Empty,
            ReceiverLastName = exchange.Receiver?.LastName ?? string.Empty,
            Status = exchange.Status,
            Rating = exchange.Rating,
            Comment = exchange.Comment ?? string.Empty,
            Created = exchange.Created,
            Modified = exchange.Modified
        });

        return Result<IEnumerable<ExchangeResponse>>.Ok(exchangeResponses);
    }

    public async Task<Result<ExchangeResponse>> GetExchangeByIdAsync(Guid exchangeId)
    {
        var includes = new List<Func<IQueryable<Exchange>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Exchange, object>>>
        {
            query => query
                .Include(e => e.Book)
                .Include(e => e.Owner)
                .Include(e => e.Receiver)
        };
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: includes,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ExchangeResponse>.Fail("Exchange not found");
        }
        var exchange = exchangeResult.Data;
        var exchangeResponse = new ExchangeResponse
        {
            Id = exchange.Id,
            BookId = exchange.Book.Id,
            BookTitle = exchange.Book.Title,
            OwnerId = exchange.OwnerId,
            OwnerFirstName = exchange.Owner?.FirstName ?? string.Empty,
            OwnerLastName = exchange.Owner?.LastName ?? string.Empty,
            ReceiverId = exchange.ReceiverId,
            ReceiverFirstName = exchange.Receiver?.FirstName ?? string.Empty,
            ReceiverLastName = exchange.Receiver?.LastName ?? string.Empty,
            Status = exchange.Status,
            Rating = exchange.Rating,
            Comment = exchange.Comment ?? string.Empty,
            Created = exchange.Created,
            Modified = exchange.Modified
        };

        return Result<ExchangeResponse>.Ok(exchangeResponse);
    }

    public async Task<Result<ExchangeResponse>> AddExchangeAsync(Guid receiverId, AddExchangeRequest request)
    {
        if (receiverId == request.OwnerId)
        {
            return Result<ExchangeResponse>.Fail("You cannot create exchange yourself");
        }

        var exchange = new Exchange
        {
            BookId = request.BookId,
            OwnerId = request.OwnerId,
            ReceiverId = receiverId,
            Status = ExchangeStatus.Pending,
            Comment = string.Empty
        };

        var addResult = await exchangeRepository.AddAsync(exchange);

        if (!addResult.Success)
        {
            return Result<ExchangeResponse>.Fail(addResult.Error);
        }

        var addedExchange = await GetExchangeByIdAsync(exchange.Id);

        await notificationService.NotifyUserAsync(request.OwnerId, new ExchangeNotification
        {
            Message = "You have exchange proposal",
            Data = addedExchange.Data
        });
        return addedExchange;
    }

    public async Task<Result<ExchangeResponse>> UpdateExchangeAsync(Guid exchangeId, Guid userId, UpdateExchangeRequest request)
    {
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            e => e.Id == exchangeId,
            includes: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result<ExchangeResponse>.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.OwnerId == userId) return await UpdateAsOwnerAsync(exchange, request);

        if (exchange.ReceiverId == userId) return await UpdateAsReceiverAsync(exchange, request);

        return Result<ExchangeResponse>.Fail("You are not authorized to update this exchange");
    }


    private async Task<Result<ExchangeResponse>> UpdateAsOwnerAsync(Exchange exchange, UpdateExchangeRequest request)
    {
        if (request.Comment != null || request.Rating.HasValue)
        {
            return Result<ExchangeResponse>.Fail("Owner cannot change rating or comment");
        }

        exchange.Status = request.Status ?? exchange.Status;
        exchange.Modified = DateTime.UtcNow;

        var updateResult = await exchangeRepository.UpdateAsync(exchange);
        if (!updateResult.Success)
        {
            return Result<ExchangeResponse>.Fail(updateResult.Error);
        }

        var addedExchange = await GetExchangeByIdAsync(exchange.Id);

        await notificationService.NotifyUserAsync(exchange.ReceiverId, new ExchangeNotification
        {
            Message = "Book owner has updated exchange status",
            Data = addedExchange.Data
        });

        return addedExchange;
    }

    private async Task<Result<ExchangeResponse>> UpdateAsReceiverAsync(Exchange exchange, UpdateExchangeRequest request)
    {
        if (request.Status.HasValue && request.Status != exchange.Status)
        {
            return Result<ExchangeResponse>.Fail("Receiver cannot change status");
        }

        if (exchange.Status == ExchangeStatus.Pending)
        {
            return Result<ExchangeResponse>.Fail("You cannot rate pending exchange");
        }

        exchange.Comment = request.Comment ?? exchange.Comment;
        exchange.Rating = request.Rating ?? exchange.Rating;
        exchange.Modified = DateTime.UtcNow;

        var updateResult = await exchangeRepository.UpdateAsync(exchange);
        
        if (!updateResult.Success)
        {
            return Result<ExchangeResponse>.Fail(updateResult.Error);
        }

        var addedExchange = await GetExchangeByIdAsync(exchange.Id);

        await notificationService.NotifyUserAsync(exchange.OwnerId, new ExchangeNotification
        {
            Message = "Receiver left a review",
            Data = addedExchange.Data
        });

        return addedExchange;
    }


    public async Task<Result> DeleteExchangeAsync(Guid exchangeId, Guid userId)
    {
        var exchangeResult = await exchangeRepository.GetSingleAsync<Exchange>(
            filter: e => e.Id == exchangeId,
            includes: null,
            selector: null
        );

        if (!exchangeResult.Success || exchangeResult.Data == null)
        {
            return Result.Fail("Exchange not found");
        }

        var exchange = exchangeResult.Data;

        if (exchange.ReceiverId != userId)
        {
            return Result.Fail("You are not authorized to delete this book");
        }
        else if (exchange.Status != ExchangeStatus.Pending)
        {
            return Result.Fail("You are not allowed to cancel exchange on this stage");
        }
        
        var deleteResult = await exchangeRepository.RemoveAsync(exchange);

        if (!deleteResult.Success)
        {
            return Result.Fail(deleteResult.Error);
        }

        return Result.Ok();
    }
}