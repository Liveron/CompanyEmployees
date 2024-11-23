using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Service.Contracts;
using Shared.DataTransferObjects;

namespace CompanyEmployees.Presentation.Controllers;

[ApiVersion("2.0", Deprecated = true)]
[Route("api/companies")]
[ApiController]
[ApiExplorerSettings(GroupName = "v2")]
public class CompaniesV2Controller(IServiceManager service) : ControllerBase
{
    private readonly IServiceManager _service = service;

    [HttpGet]
    public async Task<IActionResult> GetCompanies()
    {
        IEnumerable<CompanyDto> companies = await _service.CompanyService
            .GetAllCompaniesAsync(trackChanges: false);

        IEnumerable<string> companiesV2 = companies.Select(x => $"{x.Name} V2");

        return Ok(companiesV2);
    }
}
