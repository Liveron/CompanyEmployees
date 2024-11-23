using Contracts;
using Entities.Exceptions;
using Entities.LinkModels;
using Entities.Models;
using LoggerService;
using Mapster;
using Service.Contracts;
using Shared.DataTransferObjects;
using Shared.RequestFeatures;

namespace Service;

internal sealed class EmployeeService(IRepositoryManager repository, ILoggerManager logger,
    IEmployeeLinks employeeLinks) : IEmployeeService
{
    private readonly IRepositoryManager _repository = repository;
    private readonly ILoggerManager _logger = logger;
    private readonly IEmployeeLinks _employeeLinks = employeeLinks;

    public async Task<(LinkResponse linkResponse, MetaData metaData)> GetEmployeesAsync(
        Guid companyId, LinkParameters linkParameters, bool trackChanges)
    {
        if (!linkParameters.EmployeeParameters.ValidAgeRange)
            throw new MaxAgeRangeBadRequestException();

        await CheckIfCompanyExists(companyId, trackChanges);

        PagedList<Employee> employeesWithMetaData = await _repository.Employee
            .GetEmployeesAsync(companyId, linkParameters.EmployeeParameters, trackChanges);

        IEnumerable<EmployeeDto> employeesDto = employeesWithMetaData
            .Adapt<IEnumerable<EmployeeDto>>();

        LinkResponse links = _employeeLinks.TryGenerateLinks(employeesDto, 
            linkParameters.EmployeeParameters.Fields, companyId, linkParameters.Context);

        return (links, employeesWithMetaData.MetaData);
    }

    public async Task<EmployeeDto> GetEmployeeAsync(Guid companyId, Guid id, bool trackChanges)
    {
        await CheckIfCompanyExists(companyId, trackChanges);

        Employee employee = await GetEmployeeForCompanyAndCheckIfItExists(
            companyId, id, trackChanges);

        EmployeeDto employeeDto = employee.Adapt<EmployeeDto>(); 
        return employeeDto;
    }

    public async Task<EmployeeDto> CreateEmployeeForCompanyAsync(
        Guid companyId, EmployeeForCreationDto employeeForCreation, bool trackChanges)
    {
        await CheckIfCompanyExists(companyId, trackChanges);

        Employee employeeEntity = employeeForCreation.Adapt<Employee>();

        _repository.Employee.CreateEmployeeForCompany(companyId, employeeEntity);
        await _repository.SaveAsync();

        EmployeeDto employeeToReturn = employeeEntity.Adapt<EmployeeDto>();

        return employeeToReturn;
    }

    public async Task DeleteEmployeeForCompanyAsync(Guid companyId, Guid id, bool trackChanges)
    {
        await CheckIfCompanyExists(companyId, trackChanges);

        Employee employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(
            companyId, id, trackChanges);

        _repository.Employee.DeleteEmployee(employeeDb);
        await _repository.SaveAsync();
    }

    public async Task UpdateEmployeeForCompanyAsync(Guid companyId, Guid id, EmployeeForUpdateDto employeeForUpdate, 
        bool compTrackChanges, bool empTrackChanges)
    {
        await CheckIfCompanyExists(companyId, compTrackChanges);

        Employee employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(
            companyId, id, empTrackChanges);

        employeeForUpdate.Adapt(employeeDb);
        await _repository.SaveAsync();
    }

    public async Task<(EmployeeForUpdateDto employeeToPatch, Employee employeeEntity)> GetEmployeeForPatchAsync(
        Guid companyId, Guid id, bool compTrackChanges, bool empTrackChanges)
    {
        await CheckIfCompanyExists(companyId, compTrackChanges);

        Employee employeeDb = await GetEmployeeForCompanyAndCheckIfItExists(
            companyId, id, compTrackChanges);

        EmployeeForUpdateDto employeeToPatch = employeeDb.Adapt<EmployeeForUpdateDto>();

        return (employeeToPatch, employeeDb);
    }

    public async Task SaveChangesForPatchAsync(EmployeeForUpdateDto employeeToPatch, Employee employeeEntity)
    {
        employeeToPatch.Adapt(employeeEntity);
        await _repository.SaveAsync();
    }

    private async Task CheckIfCompanyExists(Guid companyId, bool trackChanges)
    {
        _ = await _repository.Company.GetCompanyAsync(companyId, trackChanges)
            ?? throw new CompanyNotFoundException(companyId);
    }

    private async Task<Employee> GetEmployeeForCompanyAndCheckIfItExists(
        Guid companyId, Guid id, bool trackChanges)
    {
        return await _repository.Employee.GetEmployeeAsync(companyId, id, trackChanges)
            ?? throw new EmployeeNotFoundException(id);
    }
}
