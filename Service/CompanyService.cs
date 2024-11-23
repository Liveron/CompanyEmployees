using Contracts;
using Entities.Exceptions;
using Entities.Models;
using LoggerService;
using Mapster;
using Service.Contracts;
using Shared.DataTransferObjects;

namespace Service;

internal sealed class CompanyService : ICompanyService
{
    private readonly IRepositoryManager _repository;
    private readonly ILoggerManager _logger;

    public CompanyService(IRepositoryManager repository, ILoggerManager logger)
    {
        TypeAdapterConfig<Company, CompanyDto>
            .NewConfig()
            .Map(dest => dest.FullAddress,
                src => $"{src.Address} {src.Country}");

        _repository = repository;
        _logger = logger;
    }

    public async Task<CompanyDto> CreateCompanyAsync(CompanyForCreationDto company)
    {
        Company companyEntity = company.Adapt<Company>();

        _repository.Company.CreateCompany(companyEntity);
        await _repository.SaveAsync();

        CompanyDto companyToReturn = companyEntity.Adapt<CompanyDto>();

        return companyToReturn;
    }

    public async Task<(IEnumerable<CompanyDto> companies, string ids)> CreateCompanyCollectionAsync(
        IEnumerable<CompanyForCreationDto> companyCollection)
    {
        if (companyCollection is null)
            throw new CompanyCollectionBadRequest();

        IEnumerable<Company> companyEntities = 
            [.. companyCollection.Adapt<IEnumerable<Company>>()];

        foreach (Company company in companyEntities)
        {
            _repository.Company.CreateCompany(company);
        }

        await _repository.SaveAsync();

        IEnumerable<CompanyDto> companyCollectionToReturn = 
            companyEntities.Adapt<IEnumerable<CompanyDto>>();

        string ids = string.Join(",", companyCollectionToReturn.Select(c => c.Id));

        return (companies: companyCollectionToReturn, ids);
    }

    public async Task DeleteCompanyAsync(Guid companyId, bool trackChanges)
    {
        Company company = await GetCompanyAndCheckIfItExists(companyId, trackChanges);

        _repository.Company.DeleteCompany(company);
        await _repository.SaveAsync();
    }

    public async Task<IEnumerable<CompanyDto>> GetAllCompaniesAsync(bool trackChanges)
    {
         IEnumerable<Company> companies =
             await _repository.Company.GetAllCompaniesAsync(trackChanges);

         IEnumerable<CompanyDto> companiesDto = 
                companies.Adapt<IEnumerable<CompanyDto>>();

         return [.. companiesDto];
    }

    public async Task<IEnumerable<CompanyDto>> GetByIdsAsync(IEnumerable<Guid> ids, bool trackChanges)
    {
        if (ids is null)
            throw new IdParametersBadRequestException();

        IEnumerable<Company> companyEntities = 
            await _repository.Company.GetByIdsAsync(ids, trackChanges);

        if (ids.Count() != companyEntities.Count())
            throw new CollectionByIdsBadRequestException();

        IEnumerable<CompanyDto> companiesToReturn = 
            companyEntities.Adapt<IEnumerable<CompanyDto>>();

        return companiesToReturn;
    }

    public async Task<CompanyDto?> GetCompanyAsync(Guid companyId, bool trackChanges)
    {
        Company company = await GetCompanyAndCheckIfItExists(companyId, trackChanges);

        CompanyDto companyDto = company.Adapt<CompanyDto>();

        return companyDto;
    }

    public async Task UpdateCompanyAsync(Guid companyId, CompanyForUpdateDto companyForUpdate, bool trackChanges)
    {
        Company company = await GetCompanyAndCheckIfItExists(companyId, trackChanges);

        companyForUpdate.Adapt(company);
        await _repository.SaveAsync();
    }

    private async Task<Company> GetCompanyAndCheckIfItExists(Guid companyId, bool trackChanges)
    {
        return await _repository.Company.GetCompanyAsync(companyId, trackChanges)
            ?? throw new CompanyNotFoundException(companyId);
    }
}
