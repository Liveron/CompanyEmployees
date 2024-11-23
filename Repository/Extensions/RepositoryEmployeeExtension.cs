using System.Reflection;
using System.Text;
using System.Linq.Dynamic.Core;
using Repository.Extensions.Utility;

namespace Repository.Extensions;

public static class RepositoryEmployeeExtension
{
    public static IQueryable<Employee> FilterEmployees(this IQueryable<Employee> employees,
        uint minAge, uint maxAge)
    {
        return employees.Where(e => e.Age >= minAge && e.Age <= maxAge);
    }

    public static IQueryable<Employee> Search(this IQueryable<Employee> employees,
        string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) 
            return employees;

        string lowerCaseTerm = searchTerm.Trim().ToLower();

        return employees.Where(e => e.Name.ToLower().Contains(lowerCaseTerm));
    }

    public static IQueryable<Employee> Sort(this IQueryable<Employee> employees,
        string? orderByQueryString)
    {
        if (string.IsNullOrWhiteSpace(orderByQueryString))
            return employees.OrderBy(e => e.Name);

        string orderQuery = OrderQueryBuilder
            .CreateOrderQuery<Employee>(orderByQueryString);

        if (string.IsNullOrWhiteSpace(orderQuery))
            return employees.OrderBy(e => e.Name);

        return employees.OrderBy(orderQuery);
    }
}
