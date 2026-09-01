using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagement.Api.Services;

public class BorrowRecordService : IBorrowRecordService
{
    private readonly IBorrowRecordRepository _borrowRecordRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IMemoryCache _cache;

    public BorrowRecordService(
        IBorrowRecordRepository borrowRecordRepository,
        IBookRepository bookRepository,
        IMemberRepository memberRepository,
        IMemoryCache cache)
    {
        _borrowRecordRepository = borrowRecordRepository;
        _bookRepository = bookRepository;
        _memberRepository = memberRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<BorrowRecordResponse>> GetBorrowRecordsAsync()
    {
        var records = await _borrowRecordRepository.GetAllAsync();
        return records.Select(MapToResponse);
    }

    public async Task<BorrowRecordResponse?> GetBorrowRecordByIdAsync(Guid id)
    {
        var record = await _borrowRecordRepository.GetByIdAsync(id);
        return record is null ? null : MapToResponse(record);
    }

    public async Task<IEnumerable<BorrowRecordResponse>> GetBorrowRecordsByMemberIdAsync(Guid memberId)
    {
        _ = await _memberRepository.GetByIdAsync(memberId)
            ?? throw new NotFoundException($"Member with ID {memberId} not found.");

        var records = await _borrowRecordRepository.GetByMemberIdAsync(memberId);
        return records.Select(MapToResponse);
    }

    public async Task<BorrowRecordResponse> BorrowBookAsync(CreateBorrowRequest request)
    {
        // Use a transaction to ensure all operations are atomic
        using var transaction = await _borrowRecordRepository.BeginTransactionAsync();

        try
        {
            var book = await _bookRepository.GetByIdAsync(request.BookId)
                ?? throw new NotFoundException($"Book with ID {request.BookId} not found.");

            var member = await _memberRepository.GetByIdAsync(request.MemberId)
                ?? throw new NotFoundException($"Member with ID {request.MemberId} not found.");

            // Check for active borrows within the transaction
            if (await _borrowRecordRepository.HasActiveBorrowAsync(request.MemberId, request.BookId))
                throw new ConflictException("Member already has an active borrow for this book.");

            // Atomic conditional decrement: succeeds only if AvailableCopies > 0 at the DB level.
            // This prevents two simultaneous requests from both borrowing the last copy.
            bool reserved = await _bookRepository.TryDecrementAvailableCopiesAsync(request.BookId);
            if (!reserved)
                throw new ConflictException("No copies of this book are currently available.");

            var record = new BorrowRecord
            {
                Id = Guid.NewGuid(),
                BookId = request.BookId,
                MemberId = request.MemberId,
                BorrowDate = DateTime.UtcNow,
                Status = "Borrowed",
                Book = book,
                Member = member
            };

            await _borrowRecordRepository.AddAsync(record);
            await transaction.CommitAsync();

            // Invalidate cached book data so reads reflect the updated AvailableCopies.
            _cache.Remove("books_all");
            _cache.Remove($"book_{request.BookId}");

            return MapToResponse(record);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BorrowRecordResponse> ReturnBookAsync(Guid borrowRecordId)
    {
        // Use a transaction to ensure all operations are atomic
        using var transaction = await _borrowRecordRepository.BeginTransactionAsync();

        try
        {
            var record = await _borrowRecordRepository.GetByIdAsync(borrowRecordId)
                ?? throw new NotFoundException($"Borrow record with ID {borrowRecordId} not found.");

            if (record.Status != "Borrowed")
                throw new ConflictException("This book has already been returned.");

            record.Status = "Returned";
            record.ReturnDate = DateTime.UtcNow;

            // Atomically increment available copies
            bool incremented = await _bookRepository.IncrementAvailableCopiesAsync(record.BookId);
            if (!incremented)
            {
                // This shouldn't happen with proper constraints, but handle it gracefully
                throw new ConflictException("Unable to return book - inventory constraint violation.");
            }

            var updated = await _borrowRecordRepository.UpdateAsync(record);
            await transaction.CommitAsync();

            // Invalidate cached book data
            _cache.Remove("books_all");
            _cache.Remove($"book_{record.BookId}");

            return MapToResponse(updated);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static BorrowRecordResponse MapToResponse(BorrowRecord br) => new()
    {
        Id = br.Id,
        BookId = br.BookId,
        MemberId = br.MemberId,
        BookTitle = br.Book?.Title ?? string.Empty,
        MemberFullName = br.Member?.FullName ?? string.Empty,
        BorrowDate = br.BorrowDate,
        ReturnDate = br.ReturnDate,
        Status = br.Status
    };
}
