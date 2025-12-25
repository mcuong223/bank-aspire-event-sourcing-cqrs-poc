using Account.Read.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Account.Read.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly ReadDbContext _context;

    public AccountsController(ReadDbContext context)
    {
        _context = context;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var account = await _context.Accounts.FindAsync(id);
        if (account == null)
        {
            return NotFound();
        }
        return Ok(account);
    }
}
