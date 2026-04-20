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
using Microsoft.EntityFrameworkCore.Storage;

namespace APICore.Services.Impls
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IUnitOfWork _uow;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentMethodService _paymentMethodService;
        private readonly IStringLocalizer<IOrganizationService> _localizer;

        public OrganizationService(
            IUnitOfWork uow,
            ICurrencyService currencyService,
            IPaymentMethodService paymentMethodService,
            IStringLocalizer<IOrganizationService> localizer)
        {
            _uow = uow;
            _currencyService = currencyService;
            _paymentMethodService = paymentMethodService;
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
            await _paymentMethodService.EnsureCashPaymentMethodExistsAsync(organization.Id);

            return ToResponse(organization);
        }

        public async Task DeleteOrganization(int id)
        {
            var exists = await _uow.OrganizationRepository
                .FindBy(o => o.Id == id)
                .AnyAsync();

            if (!exists)
                throw new OrganizationNotFoundException(_localizer);

            await _uow.ExecuteInTransactionAsync(async () =>
            {
                await DeleteOrganizationDataInOrder(id);

                await _uow.OrganizationRepository
                    .FindBy(o => o.Id == id)
                    .ExecuteDeleteAsync();
            });
        }

        private async Task DeleteOrganizationDataInOrder(int organizationId)
        {
            await _uow.MetricsEventRepository.GetAll()
                .IgnoreQueryFilters()
                .Where(e => e.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.DailySummaryRepository.GetAll()
                .IgnoreQueryFilters()
                .Where(d => d.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            var orgLocationIds = await _uow.LocationRepository.FindBy(l => l.OrganizationId == organizationId)
                .Select(l => l.Id)
                .ToListAsync();

            await _uow.InventoryMovementRepository.FindBy(im => 
                orgLocationIds.Contains(im.LocationId))
                .ExecuteDeleteAsync();

            await _uow.InventoryRepository.FindBy(i => 
                orgLocationIds.Contains(i.LocationId))
                .ExecuteDeleteAsync();

            await _uow.LoyaltyEventRepository.FindBy(e => e.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.CustomerLoyaltyAccountRepository.FindBy(a => a.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.LoyaltySettingsRepository.FindBy(s => s.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.SaleOrderItemRepository.FindBy(oi => 
                oi.SaleOrder != null && oi.SaleOrder.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.SaleReturnItemRepository.FindBy(ri => 
                ri.SaleReturn != null && ri.SaleReturn.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.SaleOrderRepository.FindBy(so => so.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.SaleReturnRepository.FindBy(sr => sr.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.ProductTagRepository.FindBy(pt => 
                pt.Product != null && pt.Product.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.ProductImageRepository.FindBy(pi => 
                pi.Product != null && pi.Product.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.ProductLocationOfferRepository.FindBy(plo => plo.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.ProductRepository.FindBy(p => p.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.ProductCategoryRepository.FindBy(pc => pc.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.ContactRepository.FindBy(c => c.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.UserRepository.FindBy(u => u.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.WebPushSubscriptionRepository.FindBy(wps => wps.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.LocationRepository.FindBy(l => l.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            var orgRoles = await _uow.RoleRepository.FindBy(r => r.OrganizationId == organizationId).ToListAsync();
            if (orgRoles.Any())
            {
                var roleIds = orgRoles.Select(r => r.Id).ToList();
                await _uow.RolePermissionRepository.FindBy(rp => roleIds.Contains(rp.RoleId))
                    .ExecuteDeleteAsync();
                await _uow.RoleRepository.FindBy(r => r.OrganizationId == organizationId)
                    .ExecuteDeleteAsync();
            }

            await _uow.CurrencyRepository.FindBy(c => c.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.OrganizationRepository.GetAll()
                .IgnoreQueryFilters()
                .Where(o => o.Id == organizationId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.SubscriptionId, (int?)null));

            await _uow.SubscriptionRequestRepository.FindBy(sr =>
                sr.Subscription != null && sr.Subscription.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
            await _uow.SubscriptionRepository.FindBy(s => s.OrganizationId == organizationId)
                .ExecuteDeleteAsync();

            await _uow.PromotionRepository.FindBy(p => p.OrganizationId == organizationId)
                .ExecuteDeleteAsync();
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
