using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Exceptions;
using LibraryManagement.Api.Models;
using LibraryManagement.Api.Repositories;

namespace LibraryManagement.Api.Services;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;

    public MemberService(IMemberRepository memberRepository)
    {
        _memberRepository = memberRepository;
    }

    public async Task<IEnumerable<MemberResponse>> GetMembersAsync()
    {
        var members = await _memberRepository.GetAllAsync();
        return members.Select(m => new MemberResponse
        {
            Id = m.Id,
            FullName = m.FullName,
            Email = m.Email,
            MembershipDate = m.MembershipDate
        });
    }

    public async Task<MemberResponse?> GetMemberByIdAsync(Guid id)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member is null)
            return null;

        return new MemberResponse
        {
            Id = member.Id,
            FullName = member.FullName,
            Email = member.Email,
            MembershipDate = member.MembershipDate
        };
    }

    public async Task<MemberResponse> CreateMemberAsync(CreateMemberRequest request)
    {
        if (await _memberRepository.ExistsByEmailAsync(request.Email))
            throw new ConflictException($"Member with email {request.Email} already exists.");

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Email = request.Email,
            MembershipDate = DateTime.UtcNow
        };

        var created = await _memberRepository.AddAsync(member);
        return new MemberResponse
        {
            Id = created.Id,
            FullName = created.FullName,
            Email = created.Email,
            MembershipDate = created.MembershipDate
        };
    }

    public async Task<MemberResponse?> UpdateMemberAsync(Guid id, UpdateMemberRequest request)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member is null)
            return null;

        var existingMemberWithEmail = await _memberRepository.GetByEmailAsync(request.Email);
        if (existingMemberWithEmail is not null && existingMemberWithEmail.Id != id)
            throw new ConflictException($"Member with email {request.Email} already exists.");

        member.FullName = request.FullName;
        member.Email = request.Email;
        await _memberRepository.UpdateAsync(member);

        return new MemberResponse
        {
            Id = member.Id,
            FullName = member.FullName,
            Email = member.Email,
            MembershipDate = member.MembershipDate
        };
    }

    public async Task<bool> DeleteMemberAsync(Guid id)
    {
        var member = await _memberRepository.GetByIdAsync(id);
        if (member is null)
            return false;

        await _memberRepository.DeleteAsync(member);
        return true;
    }
}
