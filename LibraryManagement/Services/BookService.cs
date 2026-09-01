using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace LibraryManagement.Api.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _bookRepository;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(2);

    public BookService(IBookRepository bookRepository, IMemoryCache cache)
    {
        _bookRepository = bookRepository;
        _cache = cache;
    }

    public async Task<IEnumerable<BookResponse>> GetBooksAsync()
    {
        if (_cache.TryGetValue("books_all", out IEnumerable<BookResponse>? cached) && cached is not null)
            return cached;

        var books = await _bookRepository.GetAllAsync();
        var response = books.Select(b => new BookResponse
        {
            Id = b.Id,
            Title = b.Title,
            Author = b.Author,
            ISBN = b.ISBN,
            TotalCopies = b.TotalCopies,
            AvailableCopies = b.AvailableCopies
        }).ToList();

        _cache.Set("books_all", response, CacheDuration);
        return response;
    }

    public async Task<BookResponse?> GetBookByIdAsync(Guid id)
    {
        string key = $"book_{id}";
        if (_cache.TryGetValue(key, out BookResponse? cached) && cached is not null)
            return cached;

        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null)
            return null;

        var response = new BookResponse
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies
        };

        _cache.Set(key, response, CacheDuration);
        return response;
    }

    public async Task<BookResponse> CreateBookAsync(CreateBookRequest request)
    {
        if (await _bookRepository.ExistsByIsbnAsync(request.ISBN))
            throw new ConflictException($"A book with ISBN '{request.ISBN}' already exists.");

        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Author = request.Author,
            ISBN = request.ISBN,
            TotalCopies = request.TotalCopies,
            AvailableCopies = request.TotalCopies
        };

        var created = await _bookRepository.AddAsync(book);
        _cache.Remove("books_all");

        return new BookResponse
        {
            Id = created.Id,
            Title = created.Title,
            Author = created.Author,
            ISBN = created.ISBN,
            TotalCopies = created.TotalCopies,
            AvailableCopies = created.AvailableCopies
        };
    }

    public async Task<BookResponse?> UpdateBookAsync(Guid id, UpdateBookRequest request)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null)
            return null;

        if (request.AvailableCopies > request.TotalCopies)
            throw new InvalidOperationException("AvailableCopies cannot exceed TotalCopies.");

        if (book.ISBN != request.ISBN && await _bookRepository.ExistsByIsbnAsync(request.ISBN))
            throw new ConflictException($"A book with ISBN '{request.ISBN}' already exists.");

        book.Title = request.Title;
        book.Author = request.Author;
        book.ISBN = request.ISBN;
        book.TotalCopies = request.TotalCopies;
        book.AvailableCopies = request.AvailableCopies;

        var updated = await _bookRepository.UpdateAsync(book);

        // Invalidate both the list cache and the individual book cache
        _cache.Remove("books_all");
        _cache.Remove($"book_{id}");

        return new BookResponse
        {
            Id = updated.Id,
            Title = updated.Title,
            Author = updated.Author,
            ISBN = updated.ISBN,
            TotalCopies = updated.TotalCopies,
            AvailableCopies = updated.AvailableCopies
        };
    }

    public async Task<bool> DeleteBookAsync(Guid id)
    {
        var book = await _bookRepository.GetByIdAsync(id);
        if (book is null)
            return false;

        await _bookRepository.DeleteAsync(book);

        // Invalidate both the list cache and the individual book cache
        _cache.Remove("books_all");
        _cache.Remove($"book_{id}");

        return true;
    }
}