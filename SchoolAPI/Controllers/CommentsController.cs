using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAPI.Data;
using SchoolAPI.Models;

namespace SchoolAPI.Controllers;

/// <summary>
/// Handles the comment section on Contact.html:
///   GET  /api/comments      — load all comments (newest first)
///   POST /api/comments      — post a new comment (requires Google login)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CommentsController(AppDbContext db)
    {
        _db = db;
    }

    // ── GET /api/comments ─────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Comment>>> GetComments()
    {
        var comments = await _db.Comments
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return Ok(comments);
    }

    // ── POST /api/comments ────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult> PostComment([FromBody] CommentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.UserName) || string.IsNullOrWhiteSpace(req.CommentText))
        {
            return BadRequest(new { error = "UserName and CommentText are required." });
        }

        var comment = new Comment
        {
            UserName    = req.UserName,
            UserEmail   = req.UserEmail,
            UserImage   = req.UserImage,
            CommentText = req.CommentText,
            CreatedAt   = DateTime.Now
        };

        _db.Comments.Add(comment);
        await _db.SaveChangesAsync();

        return StatusCode(201, new { success = true });
    }
}
