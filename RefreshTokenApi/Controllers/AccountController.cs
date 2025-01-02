using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RefreshTokenApi.Dapper.DapperService;
using RefreshTokenApi.DTO;

namespace RefreshTokenApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(Register model)
        {
            if(ModelState.IsValid)
            {
                var response = await _accountService.Register(model);
                if (response)
                {
                    return Ok(response);
                }

            }
            return NotFound();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login (Login model)
        {
            if (ModelState.IsValid)
            {
                var response = await _accountService.Login(model);
                if (response != null)
                {
                    return Ok(response);
                }

            }
            return NotFound();
        }

        [HttpPost("Refresh-Token")]
        public async Task<IActionResult> RefreshToken(Token model)
        {
            if (ModelState.IsValid)
            {
                var response = await _accountService.RefreshToken(model);
                if (response != null)
                {
                    return Ok(response);
                }

            }
            return NotFound();
        }
    }
}
