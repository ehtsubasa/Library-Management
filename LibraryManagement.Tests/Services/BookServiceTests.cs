using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;
using LibraryManagement.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace LibraryManagement.Tests.Services;

public class BookServiceTests
{
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

    private BookService CreateService() => new(_bookRepository.Object, _cache);

    private static Book MakeBook() => new()
    {
        Id = Guid.NewGuid(),
        Title = "The Pragmatic Programmer",
        Author = "David Thomas",
        ISBN = "9780135957059",
        TotalCopies = 5,
        AvailableCopies = 5
    };

    // ---------- GetBooksAsync (caching) ----------

    [Fact]
    public async Task GetBooksAsync_ShouldQueryRepository_WhenCacheIsEmpty()
    {
        var books = new List<Book> { MakeBook() };
        _bookRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(books);

        var service = CreateService();
        var result = await service.GetBooksAsync();

        Assert.Single(result);
        _bookRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetBooksAsync_ShouldReturnCachedResult_OnSubsequentCalls()
    {
        var books = new List<Book> { MakeBook() };
        _bookRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(books);

        var service = CreateService();
        await service.GetBooksAsync();
        await service.GetBooksAsync();

        _bookRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    // ---------- CreateBookAsync ----------

    [Fact]
    public async Task CreateBookAsync_ShouldThrowConflictException_WhenIsbnAlreadyExists()
    {
        var request = new CreateBookRequest { Title = "Dup", Author = "A", ISBN = "111", TotalCopies = 1 };
        _bookRepository.Setup(r => r.ExistsByIsbnAsync(request.ISBN)).ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateBookAsync(request));
        _bookRepository.Verify(r => r.AddAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task CreateBookAsync_ShouldCreateBook_WhenIsbnIsUnique()
    {
        var request = new CreateBookRequest { Title = "New Book", Author = "A", ISBN = "222", TotalCopies = 4 };
        _bookRepository.Setup(r => r.ExistsByIsbnAsync(request.ISBN)).ReturnsAsync(false);
        _bookRepository.Setup(r => r.AddAsync(It.IsAny<Book>())).ReturnsAsync((Book b) => b);

        var service = CreateService();
        var result = await service.CreateBookAsync(request);

        Assert.Equal(request.Title, result.Title);
        Assert.Equal(request.TotalCopies, result.AvailableCopies);
    }

    // ---------- UpdateBookAsync ----------

    [Fact]
    public async Task UpdateBookAsync_ShouldReturnNull_WhenBookNotFound()
    {
        var id = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Book?)null);
        var request = new UpdateBookRequest { Title = "T", Author = "A", ISBN = "1", TotalCopies = 1, AvailableCopies = 1 };

        var service = CreateService();
        var result = await service.UpdateBookAsync(id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBookAsync_ShouldThrowInvalidOperationException_WhenAvailableExceedsTotal()
    {
        var book = MakeBook();
        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);
        var request = new UpdateBookRequest
        {
            Title = book.Title,
            Author = book.Author,
            ISBN = book.ISBN,
            TotalCopies = 2,
            AvailableCopies = 5
        };

        var service = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateBookAsync(book.Id, request));
    }

    [Fact]
    public async Task UpdateBookAsync_ShouldThrowConflictException_WhenNewIsbnAlreadyUsedByAnotherBook()
    {
        var book = MakeBook();
        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);
        _bookRepository.Setup(r => r.ExistsByIsbnAsync("999")).ReturnsAsync(true);
        var request = new UpdateBookRequest { Title = "T", Author = "A", ISBN = "999", TotalCopies = 5, AvailableCopies = 5 };

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateBookAsync(book.Id, request));
    }

    // ---------- DeleteBookAsync ----------

    [Fact]
    public async Task DeleteBookAsync_ShouldReturnFalse_WhenBookNotFound()
    {
        var id = Guid.NewGuid();
        _bookRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Book?)null);

        var service = CreateService();
        var result = await service.DeleteBookAsync(id);

        Assert.False(result);
        _bookRepository.Verify(r => r.DeleteAsync(It.IsAny<Book>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBookAsync_ShouldReturnTrue_WhenBookIsDeleted()
    {
        var book = MakeBook();
        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);

        var service = CreateService();
        var result = await service.DeleteBookAsync(book.Id);

        Assert.True(result);
        _bookRepository.Verify(r => r.DeleteAsync(book), Times.Once);
    }
}
