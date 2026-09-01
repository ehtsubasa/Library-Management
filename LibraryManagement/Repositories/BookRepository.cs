using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class BookRepository : IBookRepository
{
    private readonly ApplicationDbContext _context;

    public BookRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Atomically decrements AvailableCopies if > 0. Prevents race conditions
    /// where multiple requests could borrow the last copy simultaneously.
    /// Uses SQL UPDATE with WHERE to ensure atomicity.
    /// </summary>
    public async Task<bool> TryDecrementAvailableCopiesAsync(Guid bookId)
    {
        // Use raw SQL for atomic update: only decrement if AvailableCopies > 0
        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE Books SET AvailableCopies = AvailableCopies - 1 WHERE Id = {0} AND AvailableCopies > 0",
            bookId);

        return affectedRows > 0; // Returns true if update succeeded (book was available)
    }

    /// <summary>
    /// Atomically increments AvailableCopies when a book is returned.
    /// Ensures AvailableCopies never exceeds TotalCopies.
    /// </summary>
    public async Task<bool> IncrementAvailableCopiesAsync(Guid bookId)
    {
        // Use raw SQL for atomic update: only increment if AvailableCopies < TotalCopies
        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
            "UPDATE Books SET AvailableCopies = AvailableCopies + 1 WHERE Id = {0} AND AvailableCopies < TotalCopies",
            bookId);

        return affectedRows > 0; // Returns true if update succeeded
    }

    public async Task<IEnumerable<Book>> GetAllAsync()
    {
        return await _context.Books.ToListAsync();
    }

    public async Task<Book?> GetByIdAsync(Guid id)
    {
        return await _context.Books.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Book> AddAsync(Book book)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task<Book> UpdateAsync(Book book)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync();
        return book;
    }

    public async Task DeleteAsync(Book book)
    {
        _context.Books.Remove(book);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByIsbnAsync(string isbn)
    {
        return await _context.Books.AnyAsync(b => b.ISBN == isbn);
    }
}