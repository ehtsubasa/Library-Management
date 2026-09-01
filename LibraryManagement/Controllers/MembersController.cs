using Microsoft.AspNetCore.Mvc;
using LibraryManagement.Api.Dtos;
using LibraryManagement.Api.Services;
using System.Net.Mail;

namespace LibraryManagement.Api.Controllers;

[ApiController]
[Route("api/members")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    // Helper method to validate email format
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email.Trim();
        }
        catch
        {
            return false;
        }
    }

    public MembersController(IMemberService memberService)
    {
        _memberService = memberService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MemberResponse>>> GetMembers()
    {
        var members = await _memberService.GetMembersAsync();
        return Ok(members);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MemberResponse>> GetMemberById(Guid id)
    {
        var member = await _memberService.GetMemberByIdAsync(id);
        if (member is null)
            return NotFound(new { error = $"Member with ID {id} was not found." });

        return Ok(member);
    }

    [HttpPost]
    public async Task<ActionResult<MemberResponse>> CreateMember([FromBody] CreateMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "FullName is required." });

        if (string.IsNullOrWhiteSpace(request.Email) || ! !IsValidEmail(request.Email))
            return BadRequest(new { error = "A valid Email is required." });

        var created = await _memberService.CreateMemberAsync(request);
        return CreatedAtAction(nameof(GetMemberById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MemberResponse>> UpdateMember(Guid id, [FromBody] UpdateMemberRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest(new { error = "FullName is required." });

        if (string.IsNullOrWhiteSpace(request.Email) || ! !IsValidEmail(request.Email))
            return BadRequest(new { error = "A valid Email is required." });

        var updated = await _memberService.UpdateMemberAsync(id, request);
        if (updated is null)
            return NotFound(new { error = $"Cannot update. Member with ID {id} not found." });

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteMember(Guid id)
    {
        var success = await _memberService.DeleteMemberAsync(id);
        if (!success)
            return NotFound(new { error = $"Cannot delete. Member with ID {id} not found." });

        return NoContent();
    }
}