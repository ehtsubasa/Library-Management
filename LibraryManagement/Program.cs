using LibraryManagement.Api.Data;
using LibraryManagement.Api.Middleware;
using LibraryManagement.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Invalid request body."
                    : error.ErrorMessage);

            return new BadRequestObjectResult(new
            {
                error = string.Join(" ", errors)
            });
        };
    });
builder.Services.AddMemoryCache();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<LibraryManagement.Api.Repositories.IBookRepository, LibraryManagement.Api.Repositories.BookRepository>();
builder.Services.AddScoped<LibraryManagement.Api.Services.IBookService, LibraryManagement.Api.Services.BookService>();
builder.Services.AddScoped<LibraryManagement.Api.Repositories.IMemberRepository, LibraryManagement.Api.Repositories.MemberRepository>();
builder.Services.AddScoped<LibraryManagement.Api.Services.IMemberService, LibraryManagement.Api.Services.MemberService>();
builder.Services.AddScoped<LibraryManagement.Api.Repositories.IBorrowRecordRepository, LibraryManagement.Api.Repositories.BorrowRecordRepository>();
builder.Services.AddScoped<LibraryManagement.Api.Services.IBorrowRecordService, LibraryManagement.Api.Services.BorrowRecordService>();

var app = builder.Build();

// Seed data for testing
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await SeedDataAsync(context);
}

// Global exception handling — catches unhandled exceptions from any layer
// and returns a consistent { "error": "..." } response
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
    });
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

static async Task SeedDataAsync(ApplicationDbContext context)
{
    context.Database.Migrate(); // Ensure database is created and migrated

    if (await context.Books.AnyAsync() || await context.Members.AnyAsync() || await context.BorrowRecords.AnyAsync())
    {
        return; // Data already seeded
    }

    // Seed 100 Books
    var books = new List<Book>();
    for (int i = 1; i <= 100; i++)
    {
        var totalCopies = Random.Shared.Next(5, 11);
        books.Add(new Book
        {
            Id = Guid.NewGuid(),
            Title = $"Book Title {i}",
            Author = $"Author {i}",
            ISBN = $"ISBN{i:D10}",
            TotalCopies = totalCopies,
            AvailableCopies = totalCopies
        });
    }
    await context.Books.AddRangeAsync(books);
    await context.SaveChangesAsync();

    // Seed 100 Members
    var members = new List<Member>();
    for (int i = 1; i <= 100; i++)
    {
        members.Add(new Member
        {
            Id = Guid.NewGuid(),
            FullName = $"Member {i}",
            Email = $"member{i}@example.com",
            MembershipDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365))
        });
    }
    await context.Members.AddRangeAsync(members);
    await context.SaveChangesAsync();

    // Seed 100 BorrowRecords
    var borrowRecords = new List<BorrowRecord>();
    for (int i = 1; i <= 100; i++)
    {
        var book = books[Random.Shared.Next(books.Count)];
        var member = members[Random.Shared.Next(members.Count)];
        var borrowDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30));
        var isReturned = Random.Shared.Next(2) == 0; // 50% chance
        DateTime? returnDate = null;
        string status = "Borrowed";
        if (isReturned)
        {
            returnDate = borrowDate.AddDays(Random.Shared.Next(1, 15));
            status = "Returned";
        }

        borrowRecords.Add(new BorrowRecord
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            MemberId = member.Id,
            BorrowDate = borrowDate,
            ReturnDate = returnDate,
            Status = status
        });
    }
    await context.BorrowRecords.AddRangeAsync(borrowRecords);
    await context.SaveChangesAsync();
}
