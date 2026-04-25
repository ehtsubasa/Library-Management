using LibraryManagement.Api.Data;
using LibraryManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Api.Repositories;

public class MemberRepository : IMemberRepository
{
    private readonly ApplicationDbContext _context;

    public MemberRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Member>> GetAllAsync()
    {
        return await _context.Members.ToListAsync();
    }

    public async Task<Member?> GetByIdAsync(Guid id)
    {
        return await _context.Members.FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Member?> GetByEmailAsync(string email)
    {
        return await _context.Members.FirstOrDefaultAsync(m => m.Email == email);
    }

    public async Task<Member> AddAsync(Member member)
    {
        _context.Members.Add(member);
        await _context.SaveChangesAsync();
        return member;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Members.AnyAsync(m => m.Email == email);
    }

    public async Task UpdateAsync(Member member)
    {
        _context.Members.Update(member);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Member member)
    {
        _context.Members.Remove(member);
        await _context.SaveChangesAsync();
    }
}
