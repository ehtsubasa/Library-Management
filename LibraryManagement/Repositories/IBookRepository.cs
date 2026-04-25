using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories;

public interface IBookRepository
{
    Task<IEnumerable<Book>> GetAllAsync();
    Task<Book?> GetByIdAsync(Guid id);
    Task<Book> AddAsync(Book book);
    Task<Book> UpdateAsync(Book book);
    Task DeleteAsync(Book book);
    Task<bool> ExistsByIsbnAsync(string isbn);
    Task<bool> TryDecrementAvailableCopiesAsync(Guid bookId);
    Task<bool> IncrementAvailableCopiesAsync(Guid bookId);
}