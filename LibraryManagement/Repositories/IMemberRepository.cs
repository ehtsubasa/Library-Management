using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Repositories;

public interface IMemberRepository
{
    Task<IEnumerable<Member>> GetAllAsync();
    Task<Member?> GetByIdAsync(Guid id);
    Task<Member?> GetByEmailAsync(string email);
    Task<Member> AddAsync(Member member);
    Task<bool> ExistsByEmailAsync(string email);
    Task UpdateAsync(Member member);
    Task DeleteAsync(Member member);
}
