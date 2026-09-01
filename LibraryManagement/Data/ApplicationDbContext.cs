using Microsoft.EntityFrameworkCore;
using LibraryManagement.Api.Models;

namespace LibraryManagement.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Member> Members => Set<Member>();
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

        // Database constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Book>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Books_TotalCopies", "TotalCopies > 0");
                    table.HasCheckConstraint("CK_Books_AvailableCopies", "AvailableCopies >= 0");
                    table.HasCheckConstraint("CK_Books_AvailableNotExceedTotal", "AvailableCopies <= TotalCopies");
                });
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasIndex(m => m.Email).IsUnique();
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_Members_FullName", "length(FullName) > 0");
                });
            });

            modelBuilder.Entity<BorrowRecord>(entity =>
            {
                entity.ToTable(table =>
                {
                    table.HasCheckConstraint("CK_BorrowRecords_Status", "Status IN ('Borrowed', 'Returned')");
                    table.HasCheckConstraint("CK_BorrowRecords_ReturnDate", "ReturnDate IS NULL OR ReturnDate >= BorrowDate");
                    //table.HasCheckConstraint("CK_BorrowRecords_BorrowDate", "BorrowDate <= CURRENT_TIMESTAMP");
                });
            });
        }
    }
    
}
