using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace LibraryManagement.Api.Repositories;

public class BorrowRecordRepository : IBorrowRecordRepository
{
    private readonly ApplicationDbContext _context;

    public BorrowRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BorrowRecord>> GetAllAsync()
    {
        return await _context.BorrowRecords
            .Include(br => br.Member)
            .Include(br => br.Book)
            .ToListAsync();
    }

    public async Task<BorrowRecord?> GetByIdAsync(Guid id)
    {
        return await _context.BorrowRecords
            .Include(br => br.Member)
            .Include(br => br.Book)
            .FirstOrDefaultAsync(br => br.Id == id);
    }

    public async Task<IEnumerable<BorrowRecord>> GetByMemberIdAsync(Guid memberId)
    {
        return await _context.BorrowRecords
            .Include(br => br.Book)
            .Where(br => br.MemberId == memberId)
            .ToListAsync();
    }

    public async Task<BorrowRecord> AddAsync(BorrowRecord record)
    {
        _context.BorrowRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<BorrowRecord> UpdateAsync(BorrowRecord record)
    {
        _context.BorrowRecords.Update(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<bool> HasActiveBorrowAsync(Guid memberId, Guid bookId)
    {
        return await _context.BorrowRecords.AnyAsync(br =>
            br.MemberId == memberId &&
            br.BookId == bookId &&
            br.Status == "Borrowed");
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }
}