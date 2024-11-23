using Contracts;
using Entities.ConfigurationModels;
using Entities.Models;
using LoggerService;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Service.Contracts;

namespace Service;

public class ServiceManager(
    IRepositoryManager repository, ILoggerManager logger, 
    IEmployeeLinks employeeLinks, UserManager<User> userManager,
    IOptions<JwtConfiguration> configuration) 
    : IServiceManager
{
    private readonly Lazy<ICompanyService> _companyService = 
        new(() => new CompanyService(repository, logger));
    private readonly Lazy<IEmployeeService> _employeeService = 
        new(() => new EmployeeService(repository,logger, employeeLinks));
    private readonly Lazy<IAuthenticationService> _authenticationService =
        new(() => new AuthenticationService(logger, userManager, configuration));

    public ICompanyService CompanyService => _companyService.Value;
    public IEmployeeService EmployeeService => _employeeService.Value;
    public IAuthenticationService AuthenticationService => _authenticationService.Value;
}
