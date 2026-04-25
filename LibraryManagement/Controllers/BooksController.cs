using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Services;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BookResponse>>> GetBooks()
    {
        var books = await _bookService.GetBooksAsync();
        return Ok(books);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookResponse>> GetBookById(Guid id)
    {
        var response = await _bookService.GetBookByIdAsync(id);
        if (response is null)
            return NotFound(new { error = $"Book with ID {id} not found." });

        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<BookResponse>> CreateBook([FromBody] CreateBookRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) ||
            string.IsNullOrWhiteSpace(input.Author) ||
            string.IsNullOrWhiteSpace(input.ISBN))
            return BadRequest(new { error = "Title, Author, and ISBN are required." });

        if (input.TotalCopies <= 0)
            return BadRequest(new { error = "TotalCopies must be greater than 0." });

        var created = await _bookService.CreateBookAsync(input);
        return CreatedAtAction(nameof(GetBookById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BookResponse>> UpdateBook(Guid id, [FromBody] UpdateBookRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Title) ||
            string.IsNullOrWhiteSpace(input.Author) ||
            string.IsNullOrWhiteSpace(input.ISBN))
            return BadRequest(new { error = "Title, Author, and ISBN are required." });

        if (input.TotalCopies <= 0)
            return BadRequest(new { error = "TotalCopies must be greater than 0." });

        if (input.AvailableCopies < 0)
            return BadRequest(new { error = "AvailableCopies must be greater than or equal to 0." });

        if (input.AvailableCopies > input.TotalCopies)
            return BadRequest(new { error = "AvailableCopies cannot exceed TotalCopies." });

        var updated = await _bookService.UpdateBookAsync(id, input);
        if (updated is null)
            return NotFound(new { error = $"Book with ID {id} not found." });

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteBook(Guid id)
    {
        var deleted = await _bookService.DeleteBookAsync(id);
        if (!deleted)
            return NotFound(new { error = $"Cannot delete. Book with ID {id} not found." });

        return NoContent();
    }
}
