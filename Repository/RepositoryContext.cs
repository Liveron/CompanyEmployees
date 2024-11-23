using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Repository.Configuration;

namespace Repository;

public class RepositoryContext(DbContextOptions options) : IdentityDbContext<User>(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfiguration(new CompanyConfiguration())
            .ApplyConfiguration(new EmployeeConfiguration())
            .ApplyConfiguration(new RoleConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Employee> Employees { get; set; }
}
