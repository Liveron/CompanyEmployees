namespace Entities.Models;

public class Company
{
    [Column("CompanyId")]
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Company name is a required field.")]
    [MaxLength(60, ErrorMessage = "Maximum length for the Name is 60 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company address is a required field.")]
    [MaxLength(60, ErrorMessage = "Maximum length for the Address is 60 characters")]
    public string Address { get; set; } = string.Empty;

    public string? Country { get; set; }

    public ICollection<Employee>? Employees { get; set; }
}
