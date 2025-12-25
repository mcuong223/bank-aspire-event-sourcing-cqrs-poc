using Account.Command.Application;
using Microsoft.AspNetCore.Mvc;

namespace Account.Command.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly AccountService _service;

    public AccountsController(AccountService service)
    {
        _service = service;
    }

    [HttpPost("deposit")]
    public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
    {
        await _service.DepositAsync(request.AccountId, request.Amount);
        return Ok();
    }

    [HttpPost("withdraw")]
    public async Task<IActionResult> Withdraw([FromBody] WithdrawRequest request)
    {
        try
        {
            await _service.WithdrawAsync(request.AccountId, request.Amount);
            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

public record DepositRequest(Guid AccountId, decimal Amount);
public record WithdrawRequest(Guid AccountId, decimal Amount);
