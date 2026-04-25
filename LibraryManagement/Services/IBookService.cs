using LibraryManagement.Api.Dtos;

namespace LibraryManagement.Api.Services;

public interface IBookService
{
    Task<IEnumerable<BookResponse>> GetBooksAsync();
    Task<BookResponse?> GetBookByIdAsync(Guid id);
    Task<BookResponse> CreateBookAsync(CreateBookRequest request);
    Task<BookResponse?> UpdateBookAsync(Guid id, UpdateBookRequest request);
    Task<bool> DeleteBookAsync(Guid id);
}
