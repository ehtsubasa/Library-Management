using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;
using LibraryManagement.Api.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace LibraryManagement.Tests.Services;

public class BorrowRecordServiceTests
{
    private readonly Mock<IBorrowRecordRepository> _borrowRecordRepository = new();
    private readonly Mock<IBookRepository> _bookRepository = new();
    private readonly Mock<IMemberRepository> _memberRepository = new();
    private readonly IMemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    private readonly Mock<IDbContextTransaction> _transaction = new();

    private BorrowRecordService CreateService() =>
        new(_borrowRecordRepository.Object, _bookRepository.Object, _memberRepository.Object, _cache);

    public BorrowRecordServiceTests()
    {
        _borrowRecordRepository.Setup(r => r.BeginTransactionAsync()).ReturnsAsync(_transaction.Object);
    }

    private static Book MakeBook(int availableCopies = 1) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Clean Code",
        Author = "Robert C. Martin",
        ISBN = "9780132350884",
        TotalCopies = 3,
        AvailableCopies = availableCopies
    };

    private static Member MakeMember() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Jane Doe",
        Email = "jane@example.com",
        MembershipDate = DateTime.UtcNow
    };

    // ---------- BorrowBookAsync ----------

    [Fact]
    public async Task BorrowBookAsync_ShouldReturnBorrowRecord_WhenRequestIsValid()
    {
        var book = MakeBook();
        var member = MakeMember();
        var request = new CreateBorrowRequest { BookId = book.Id, MemberId = member.Id };

        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _borrowRecordRepository.Setup(r => r.HasActiveBorrowAsync(member.Id, book.Id)).ReturnsAsync(false);
        _bookRepository.Setup(r => r.TryDecrementAvailableCopiesAsync(book.Id)).ReturnsAsync(true);
        _borrowRecordRepository.Setup(r => r.AddAsync(It.IsAny<BorrowRecord>()))
            .ReturnsAsync((BorrowRecord br) => br);

        var service = CreateService();
        var result = await service.BorrowBookAsync(request);

        Assert.Equal(book.Id, result.BookId);
        Assert.Equal(member.Id, result.MemberId);
        Assert.Equal("Borrowed", result.Status);
        _transaction.Verify(t => t.CommitAsync(default), Times.Once);
        _transaction.Verify(t => t.RollbackAsync(default), Times.Never);
    }

    [Fact]
    public async Task BorrowBookAsync_ShouldThrowNotFoundException_WhenBookDoesNotExist()
    {
        var request = new CreateBorrowRequest { BookId = Guid.NewGuid(), MemberId = Guid.NewGuid() };
        _bookRepository.Setup(r => r.GetByIdAsync(request.BookId)).ReturnsAsync((Book?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.BorrowBookAsync(request));
        _transaction.Verify(t => t.RollbackAsync(default), Times.Once);
        _transaction.Verify(t => t.CommitAsync(default), Times.Never);
    }

    [Fact]
    public async Task BorrowBookAsync_ShouldThrowConflictException_WhenMemberAlreadyHasActiveBorrow()
    {
        var book = MakeBook();
        var member = MakeMember();
        var request = new CreateBorrowRequest { BookId = book.Id, MemberId = member.Id };

        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _borrowRecordRepository.Setup(r => r.HasActiveBorrowAsync(member.Id, book.Id)).ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.BorrowBookAsync(request));
        _bookRepository.Verify(r => r.TryDecrementAvailableCopiesAsync(It.IsAny<Guid>()), Times.Never);
        _transaction.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task BorrowBookAsync_ShouldThrowConflictException_WhenNoCopiesAvailable()
    {
        var book = MakeBook(availableCopies: 0);
        var member = MakeMember();
        var request = new CreateBorrowRequest { BookId = book.Id, MemberId = member.Id };

        _bookRepository.Setup(r => r.GetByIdAsync(book.Id)).ReturnsAsync(book);
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _borrowRecordRepository.Setup(r => r.HasActiveBorrowAsync(member.Id, book.Id)).ReturnsAsync(false);
        _bookRepository.Setup(r => r.TryDecrementAvailableCopiesAsync(book.Id)).ReturnsAsync(false);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.BorrowBookAsync(request));
        _borrowRecordRepository.Verify(r => r.AddAsync(It.IsAny<BorrowRecord>()), Times.Never);
        _transaction.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    // ---------- ReturnBookAsync ----------

    [Fact]
    public async Task ReturnBookAsync_ShouldMarkRecordReturned_AndIncrementAvailableCopies()
    {
        var book = MakeBook(availableCopies: 0);
        var member = MakeMember();
        var record = new BorrowRecord
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = DateTime.UtcNow.AddDays(-1),
            Status = "Borrowed",
            Book = book,
            Member = member
        };

        _borrowRecordRepository.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);
        _bookRepository.Setup(r => r.IncrementAvailableCopiesAsync(book.Id)).ReturnsAsync(true);
        _borrowRecordRepository.Setup(r => r.UpdateAsync(It.IsAny<BorrowRecord>()))
            .ReturnsAsync((BorrowRecord br) => br);

        var service = CreateService();
        var result = await service.ReturnBookAsync(record.Id);

        Assert.Equal("Returned", result.Status);
        Assert.NotNull(result.ReturnDate);
        _bookRepository.Verify(r => r.IncrementAvailableCopiesAsync(book.Id), Times.Once);
        _transaction.Verify(t => t.CommitAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReturnBookAsync_ShouldThrowConflictException_WhenAlreadyReturned()
    {
        var record = new BorrowRecord
        {
            Id = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            MemberId = Guid.NewGuid(),
            BorrowDate = DateTime.UtcNow.AddDays(-2),
            ReturnDate = DateTime.UtcNow.AddDays(-1),
            Status = "Returned"
        };

        _borrowRecordRepository.Setup(r => r.GetByIdAsync(record.Id)).ReturnsAsync(record);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.ReturnBookAsync(record.Id));
        _bookRepository.Verify(r => r.IncrementAvailableCopiesAsync(It.IsAny<Guid>()), Times.Never);
        _transaction.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    [Fact]
    public async Task ReturnBookAsync_ShouldThrowNotFoundException_WhenRecordDoesNotExist()
    {
        var id = Guid.NewGuid();
        _borrowRecordRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((BorrowRecord?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReturnBookAsync(id));
        _transaction.Verify(t => t.RollbackAsync(default), Times.Once);
    }

    // ---------- GetBorrowRecordsByMemberIdAsync ----------

    [Fact]
    public async Task GetBorrowRecordsByMemberIdAsync_ShouldThrowNotFoundException_WhenMemberDoesNotExist()
    {
        var memberId = Guid.NewGuid();
        _memberRepository.Setup(r => r.GetByIdAsync(memberId)).ReturnsAsync((Member?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetBorrowRecordsByMemberIdAsync(memberId));
    }

    [Fact]
    public async Task GetBorrowRecordsByMemberIdAsync_ShouldReturnRecords_WhenMemberExists()
    {
        var member = MakeMember();
        var book = MakeBook();
        var records = new List<BorrowRecord>
        {
            new()
            {
                Id = Guid.NewGuid(),
                BookId = book.Id,
                MemberId = member.Id,
                BorrowDate = DateTime.UtcNow,
                Status = "Borrowed",
                Book = book,
                Member = member
            }
        };

        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _borrowRecordRepository.Setup(r => r.GetByMemberIdAsync(member.Id)).ReturnsAsync(records);

        var service = CreateService();
        var result = await service.GetBorrowRecordsByMemberIdAsync(member.Id);

        Assert.Single(result);
    }
}
