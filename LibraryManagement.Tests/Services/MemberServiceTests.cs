using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;
using LibraryManagement.Api.Services;
using Moq;

namespace LibraryManagement.Tests.Services;

public class MemberServiceTests
{
    private readonly Mock<IMemberRepository> _memberRepository = new();

    private MemberService CreateService() => new(_memberRepository.Object);

    private static Member MakeMember() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "John Smith",
        Email = "john@example.com",
        MembershipDate = DateTime.UtcNow
    };

    // ---------- CreateMemberAsync ----------

    [Fact]
    public async Task CreateMemberAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
    {
        var request = new CreateMemberRequest { FullName = "Jane Doe", Email = "jane@example.com" };
        _memberRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateMemberAsync(request));
        _memberRepository.Verify(r => r.AddAsync(It.IsAny<Member>()), Times.Never);
    }

    [Fact]
    public async Task CreateMemberAsync_ShouldCreateMember_WhenEmailIsUnique()
    {
        var request = new CreateMemberRequest { FullName = "Jane Doe", Email = "jane@example.com" };
        _memberRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
        _memberRepository.Setup(r => r.AddAsync(It.IsAny<Member>())).ReturnsAsync((Member m) => m);

        var service = CreateService();
        var result = await service.CreateMemberAsync(request);

        Assert.Equal(request.FullName, result.FullName);
        Assert.Equal(request.Email, result.Email);
    }

    // ---------- UpdateMemberAsync ----------

    [Fact]
    public async Task UpdateMemberAsync_ShouldReturnNull_WhenMemberNotFound()
    {
        var id = Guid.NewGuid();
        _memberRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Member?)null);
        var request = new UpdateMemberRequest { FullName = "X", Email = "x@example.com" };

        var service = CreateService();
        var result = await service.UpdateMemberAsync(id, request);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateMemberAsync_ShouldThrowConflictException_WhenEmailBelongsToAnotherMember()
    {
        var member = MakeMember();
        var otherMember = MakeMember();
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _memberRepository.Setup(r => r.GetByEmailAsync(otherMember.Email)).ReturnsAsync(otherMember);
        var request = new UpdateMemberRequest { FullName = "Updated", Email = otherMember.Email };

        var service = CreateService();

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateMemberAsync(member.Id, request));
    }

    [Fact]
    public async Task UpdateMemberAsync_ShouldUpdateMember_WhenEmailUnchanged()
    {
        var member = MakeMember();
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);
        _memberRepository.Setup(r => r.GetByEmailAsync(member.Email)).ReturnsAsync(member);
        var request = new UpdateMemberRequest { FullName = "Updated Name", Email = member.Email };

        var service = CreateService();
        var result = await service.UpdateMemberAsync(member.Id, request);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result!.FullName);
        _memberRepository.Verify(r => r.UpdateAsync(member), Times.Once);
    }

    // ---------- DeleteMemberAsync ----------

    [Fact]
    public async Task DeleteMemberAsync_ShouldReturnFalse_WhenMemberNotFound()
    {
        var id = Guid.NewGuid();
        _memberRepository.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Member?)null);

        var service = CreateService();
        var result = await service.DeleteMemberAsync(id);

        Assert.False(result);
        _memberRepository.Verify(r => r.DeleteAsync(It.IsAny<Member>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMemberAsync_ShouldReturnTrue_WhenMemberIsDeleted()
    {
        var member = MakeMember();
        _memberRepository.Setup(r => r.GetByIdAsync(member.Id)).ReturnsAsync(member);

        var service = CreateService();
        var result = await service.DeleteMemberAsync(member.Id);

        Assert.True(result);
        _memberRepository.Verify(r => r.DeleteAsync(member), Times.Once);
    }
}
