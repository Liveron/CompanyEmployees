using CompanyEmployees.Presentation.ActionFilters;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects;

namespace CompanyEmployees.Presentation.Controllers;

[Route("api/authentication")]
[ApiController]
public class AuthenticationController(IServiceManager service) : ControllerBase
{
    private readonly IServiceManager _service = service;

    [HttpPost]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> RegisterUser([FromBody] UserForRegistrationDto userForRegistration)
    {
        IdentityResult result = await _service.AuthenticationService.RegisterUser(userForRegistration);

        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.TryAddModelError(error.Code, error.Description);
            }

            return BadRequest(ModelState);
        }

        return StatusCode(201);
    }

    [HttpPost("login")]
    [ServiceFilter(typeof(ValidationFilterAttribute))]
    public async Task<IActionResult> Authenticate([FromBody] UserForAuthenticationDto user)
    {
        if (!await _service.AuthenticationService.ValidateUser(user))
            return Unauthorized();

        TokenDto tokenDto = await _service.AuthenticationService
            .CreateToken(populateExp: true);

        return Ok(tokenDto);
    }
}
