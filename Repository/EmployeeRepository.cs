using Repository.Extensions;
using Shared.RequestFeatures;

namespace Repository;

public class EmployeeRepository(RepositoryContext repositoryContext)
    : RepositoryBase<Employee>(repositoryContext), IEmployeeRepository
{
    public async Task<PagedList<Employee>> GetEmployeesAsync(Guid companyId, 
        EmployeeParameters employeeParameters, bool trackChanges)
    {
        List<Employee> employees = await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges)
            .FilterEmployees(employeeParameters.MinAge, employeeParameters.MaxAge)
            .Search(employeeParameters.SearchTerm)
            .Sort(employeeParameters.OrderBy)
            .OrderBy(e => e.Name)
            .Skip((employeeParameters.PageNumber - 1) * employeeParameters.PageSize)
            .Take(employeeParameters.PageSize)
            .ToListAsync();

        int count = await FindByCondition(e => e.CompanyId.Equals(companyId), trackChanges).CountAsync();

        return new PagedList<Employee>(employees, count, employeeParameters.PageNumber, employeeParameters.PageSize);
    }

    public async Task<Employee?> GetEmployeeAsync(Guid companyId, Guid id, bool trackChanges)
    {
        return await FindByCondition(e => e.CompanyId.Equals(companyId) && e.Id.Equals(id), trackChanges)
            .SingleOrDefaultAsync();
    }

    public void CreateEmployeeForCompany(Guid companyId, Employee employee)
    {
        employee.CompanyId = companyId;
        Create(employee);
    }

    public void DeleteEmployee(Employee employee)
    {
        Delete(employee);
    }
}
