using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Services;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrencyService _currencyService;
        private readonly IStringLocalizer<IOrganizationService> _localizer;

        public OrganizationService(IUnitOfWork uow, ICurrencyService currencyService, IStringLocalizer<IOrganizationService> localizer)
        {
            _uow = uow;
            _currencyService = currencyService;
            _localizer = localizer;
        }

        public async Task<OrganizationResponse> CreateOrganization(CreateOrganizationRequest request)
        {
            var codeExists = await _uow.OrganizationRepository.FindBy(o => o.Code == request.Code).AnyAsync();
            if (codeExists)
                throw new OrganizationCodeInUseBadRequestException(_localizer);

            var organization = new Organization
            {
                Name = request.Name,
                Code = request.Code.Trim(),
                Description = request.Description?.Trim(),
                IsVerified = false,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.OrganizationRepository.AddAsync(organization);
            await _uow.CommitAsync();

            await _currencyService.EnsureBaseCurrencyForOrganizationAsync(organization.Id);

            return ToResponse(organization);
        }

        public async Task DeleteOrganization(int id)
        {
            var organization = await _uow.OrganizationRepository.FirstOrDefaultAsync(o => o.Id == id);
            if (organization == null)
                throw new OrganizationNotFoundException(_localizer);

            var hasLocations = await _uow.LocationRepository.FindBy(l => l.OrganizationId == id).AnyAsync();
            if (hasLocations)
                throw new OrganizationInUseCannotDeleteBadRequestException(_localizer);

            _uow.OrganizationRepository.Delete(organization);
            await _uow.CommitAsync();
        }

        public async Task<OrganizationResponse> GetOrganization(int id)
        {
            var organization = await _uow.OrganizationRepository.GetAll()
                .Include(o => o.Locations)
                .ThenInclude(l => l.BusinessCategory)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (organization == null)
                throw new OrganizationNotFoundException(_localizer);
            return ToResponse(organization, includeLocations: true);
        }

        public async Task<PaginatedList<OrganizationResponse>> GetAllOrganizations(int? page, int? perPage, string sortOrder = null)
        {
            var query = _uow.OrganizationRepository.GetAll()
                .Include(o => o.Locations)
                .ThenInclude(l => l.BusinessCategory)
                .OrderBy(o => o.Code);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            var paged = await PaginatedList<Organization>.CreateAsync(query, pageIndex, perPageIndex);
            var items = paged.ConvertAll(o => ToResponse(o, includeLocations: true));
            return new PaginatedList<OrganizationResponse>(items, paged.TotalItems, pageIndex, perPageIndex);
        }

        public async Task SetOrganizationVerification(int organizationId, bool isVerified)
        {
            var exists = await _uow.OrganizationRepository.GetAll()
                .AnyAsync(o => o.Id == organizationId);
            if (!exists)
                throw new OrganizationNotFoundException(_localizer);

            var now = DateTime.UtcNow;

            await _uow.LocationRepository.FindBy(l => l.OrganizationId == organizationId)
                .IgnoreQueryFilters()
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.IsVerified, isVerified)
                    .SetProperty(l => l.ModifiedAt, now));

            await _uow.OrganizationRepository.GetAll()
                .Where(o => o.Id == organizationId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(o => o.IsVerified, isVerified)
                    .SetProperty(o => o.ModifiedAt, now));
        }

        public async Task UpdateOrganization(int id, UpdateOrganizationRequest request)
        {
            var organization = await _uow.OrganizationRepository.FirstOrDefaultAsync(o => o.Id == id);
            if (organization == null)
                throw new OrganizationNotFoundException(_localizer);

            if (request.Name != null)
                organization.Name = request.Name;
            if (request.Code != null)
            {
                var code = request.Code.Trim();
                var codeExists = await _uow.OrganizationRepository.FindBy(o => o.Code == code && o.Id != id).AnyAsync();
                if (codeExists)
                    throw new OrganizationCodeInUseBadRequestException(_localizer);
                organization.Code = code;
            }
            if (request.Description != null)
                organization.Description = request.Description;

            organization.ModifiedAt = DateTime.UtcNow;
            _uow.OrganizationRepository.Update(organization);
            await _uow.CommitAsync();
        }

        private static OrganizationResponse ToResponse(Organization organization, bool includeLocations = false)
        {
            var response = new OrganizationResponse
            {
                Id = organization.Id,
                Name = organization.Name,
                Code = organization.Code,
                Description = organization.Description,
                IsVerified = organization.IsVerified,
                CreatedAt = organization.CreatedAt,
                ModifiedAt = organization.ModifiedAt,
            };
            if (includeLocations && organization.Locations != null)
            {
                response.Locations = organization.Locations
                    .OrderBy(l => l.Code)
                    .Select(l => new LocationResponse
                    {
                        Id = l.Id,
                        OrganizationId = l.OrganizationId,
                        OrganizationName = organization.Name,
                        Name = l.Name,
                        Code = l.Code,
                        Description = l.Description,
                        BusinessCategoryId = l.BusinessCategoryId,
                        BusinessCategory = l.BusinessCategory != null
                            ? new BusinessCategorySummaryDto { Name = l.BusinessCategory.Name, Icon = l.BusinessCategory.Icon }
                            : null,
                        IsVerified = l.IsVerified,
                        CreatedAt = l.CreatedAt,
                        ModifiedAt = l.ModifiedAt,
                    })
                    .ToList();
            }
            return response;
        }
    }
}
