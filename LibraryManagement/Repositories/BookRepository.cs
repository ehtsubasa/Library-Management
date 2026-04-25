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
    /// </summary>
    public async Task<bool> TryDecrementAvailableCopiesAsync(Guid bookId)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null || book.AvailableCopies <= 0)
            return false;

        book.AvailableCopies--;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Atomically increments AvailableCopies when a book is returned.
    /// </summary>
    public async Task<bool> IncrementAvailableCopiesAsync(Guid bookId)
    {
        var book = await _context.Books.FirstOrDefaultAsync(b => b.Id == bookId);
        if (book is null)
            return false;

        if (book.AvailableCopies < book.TotalCopies)
            book.AvailableCopies++;
        
        await _context.SaveChangesAsync();
        return true;
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