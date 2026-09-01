## Library Book Borrowing System
A RESTful backend API for managing library books, members, and borrowing — built with ASP.NET Core (.NET 10), EF Core 8, and SQLite.


## Highlights
- Clean 3-layer architecture — Controller → Service → Repository, with interfaces throughout for testability
- Race-condition-safe borrowing — atomic SQL update prevents two users from borrowing the last copy of a book at once
- DB-enforced data integrity — CHECK constraints (e.g. AvailableCopies <= TotalCopies) as a second layer beyond app validation
- Centralized error handling — one middleware converts domain exceptions into consistent { "error": "..." } JSON responses
- In-memory caching on read-heavy endpoints, with explicit invalidation on writes

  
## Tech Stack
ASP.NET Core (.NET 10) · EF Core 8 · SQLite · IMemoryCache · Swagger/Scalar


## Architecture
Client → ExceptionMiddleware → Controller → Service → Repository → Database
Controllers — validate input, delegate to services
Services — business rules (availability checks, duplicate detection, conflicts)
Repositories — all DB access via EF Core
Concurrency Handling:
  Two people trying to borrow the last copy of a book at the same time is a classic race condition. This is solved with a single atomic SQL statement instead of a read-then-write:
  sql:
UPDATE Books SET AvailableCopies = AvailableCopies - 1
WHERE Id = @bookId AND AvailableCopies > 0
Only one concurrent request can succeed; the other gets 0 affected rows and returns 409 Conflict. Borrow/return operations also run inside a DB transaction.


## API Overview
Key Endpoints:

Books:	/api/books	GET, GET/{id}, POST, PUT/{id}, DELETE/{id}

Members:	/api/members	GET, GET/{id}, POST, PUT/{id}, DELETE/{id}

Borrowing:	/api/borrowing	GET, GET/{id}, GET/history/{memberId}, POST/borrow, POST/return/{id}

Full request/response schemas available via Swagger/Scalar at runtime (/scalar/v1).
Notable business rules: 
- ISBNs and member emails must be unique ·
- a book can't be borrowed with 0 copies available ·
- a member can't double-borrow the same book ·
- a borrow record can only be returned once.


## Run Locally
dotnet restore
dotnet ef database update
dotnet run
API starts at http://localhost:XXXX, seeded with 100 sample books/members/records.

## License
This project was built as a learning/coursework exercise and is not licensed for production use.
