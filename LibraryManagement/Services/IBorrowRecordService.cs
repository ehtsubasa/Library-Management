using LibraryManagement.Api.Dtos;

namespace LibraryManagement.Api.Services;

public interface IBorrowRecordService
{
    Task<IEnumerable<BorrowRecordResponse>> GetBorrowRecordsAsync();
    Task<BorrowRecordResponse?> GetBorrowRecordByIdAsync(Guid id);
    Task<IEnumerable<BorrowRecordResponse>> GetBorrowRecordsByMemberIdAsync(Guid memberId);
    Task<BorrowRecordResponse> BorrowBookAsync(CreateBorrowRequest request);
    Task<BorrowRecordResponse> ReturnBookAsync(Guid borrowRecordId);
}
