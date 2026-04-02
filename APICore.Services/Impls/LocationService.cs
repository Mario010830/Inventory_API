#nullable enable
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace APICore.Services.Impls
{
    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<ILocationService> _localizer;
        private readonly ISubscriptionQuotaService _subscriptionQuotaService;

        public LocationService(IUnitOfWork uow, IStringLocalizer<ILocationService> localizer, ISubscriptionQuotaService subscriptionQuotaService)
        {
            _uow = uow;
            _localizer = localizer;
            _subscriptionQuotaService = subscriptionQuotaService ?? throw new ArgumentNullException(nameof(subscriptionQuotaService));
        }

        public async Task<LocationResponse> CreateLocation(CreateLocationRequest request)
        {
            var organization = await _uow.OrganizationRepository.FirstOrDefaultAsync(o => o.Id == request.OrganizationId);
            if (organization == null)
                throw new OrganizationNotFoundException(_localizer);

            var codeExists = await _uow.LocationRepository.FindBy(l => l.Code == request.Code).AnyAsync();
            if (codeExists)
                throw new LocationCodeInUseBadRequestException(_localizer);

            await _subscriptionQuotaService.EnsureCanAddLocationAsync(request.OrganizationId);

            if (request.BusinessCategoryId.HasValue && request.BusinessCategoryId.Value > 0)
            {
                var bc = await _uow.BusinessCategoryRepository.GetAsync(request.BusinessCategoryId.Value);
                if (bc == null)
                    throw new BusinessCategoryNotFoundException();
            }

            var location = new Location
            {
                OrganizationId = request.OrganizationId,
                BusinessCategoryId = request.BusinessCategoryId.HasValue && request.BusinessCategoryId.Value > 0
                    ? request.BusinessCategoryId
                    : null,
                Name = request.Name,
                Code = request.Code.Trim(),
                Description = request.Description?.Trim(),
                WhatsAppContact = request.WhatsAppContact?.Trim(),
                PhotoUrl = request.PhotoUrl?.Trim(),
                Province = request.Province?.Trim(),
                Municipality = request.Municipality?.Trim(),
                Street = request.Street?.Trim(),
                BusinessHoursJson = SerializeBusinessHours(request.BusinessHours),
                Latitude = request.Coordinates?.Lat,
                Longitude = request.Coordinates?.Lng,
                IsVerified = organization.IsVerified,
                OffersDelivery = request.OffersDelivery,
                OffersPickup = request.OffersPickup,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.LocationRepository.AddAsync(location);
            await _uow.CommitAsync();

            var created = await _uow.LocationRepository.FindBy(l => l.Id == location.Id)
                .Include(l => l.Organization)
                .Include(l => l.BusinessCategory)
                .FirstAsync();
            return ToResponse(created, created.Organization ?? organization);
        }

        public async Task DeleteLocation(int id)
        {
            var location = await _uow.LocationRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (location == null)
                throw new LocationNotFoundException(_localizer);

            
            var hasSales = await _uow.SaleOrderRepository.FindBy(s => s.LocationId == id).AnyAsync();
            var hasReturns = await _uow.SaleReturnRepository.FindBy(r => r.LocationId == id).AnyAsync();
            if (hasSales || hasReturns)
                throw new LocationInUseCannotDeleteBadRequestException(_localizer);

            
            var usersInLocation = await _uow.UserRepository.FindBy(u => u.LocationId == id).ToListAsync();
            foreach (var user in usersInLocation)
            {
                user.LocationId = null;
                _uow.UserRepository.Update(user);
            }

         
            var movements = await _uow.InventoryMovementRepository.FindBy(m => m.LocationId == id).ToListAsync();
            foreach (var movement in movements)
                _uow.InventoryMovementRepository.Delete(movement);

           
            var inventories = await _uow.InventoryRepository.FindBy(i => i.LocationId == id).ToListAsync();
            foreach (var inventory in inventories)
                _uow.InventoryRepository.Delete(inventory);

            _uow.LocationRepository.Delete(location);
            await _uow.CommitAsync();
        }

        public async Task<LocationResponse> GetLocation(int id)
        {
            var location = await _uow.LocationRepository.FindBy(l => l.Id == id)
                .Include(l => l.Organization)
                .Include(l => l.BusinessCategory)
                .FirstOrDefaultAsync();
            if (location == null)
                throw new LocationNotFoundException(_localizer);
            return ToResponse(location, location.Organization);
        }

        public async Task<PaginatedList<LocationResponse>> GetAllLocations(int? page, int? perPage, int? organizationId = null, string sortOrder = null)
        {
            var query = _uow.LocationRepository.GetAllIncluding(l => l.Organization, l => l.BusinessCategory).AsQueryable();
            if (organizationId.HasValue)
                query = query.Where(l => l.OrganizationId == organizationId.Value);
            query = query.OrderBy(l => l.Code);
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            var paged = await PaginatedList<Location>.CreateAsync(query, pageIndex, perPageIndex);
            var items = paged.Select(l => ToResponse(l, l.Organization)).ToList();
            return new PaginatedList<LocationResponse>(items, paged.TotalItems, pageIndex, perPageIndex);
        }

        public async Task UpdateLocation(int id, UpdateLocationRequest request)
        {
            var location = await _uow.LocationRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (location == null)
                throw new LocationNotFoundException(_localizer);

            if (request.OrganizationId.HasValue)
            {
                var organization = await _uow.OrganizationRepository.FirstOrDefaultAsync(o => o.Id == request.OrganizationId.Value);
                if (organization == null)
                    throw new OrganizationNotFoundException(_localizer);
                location.OrganizationId = request.OrganizationId.Value;
            }
            if (request.Name != null)
                location.Name = request.Name;
            if (request.Code != null)
            {
                var code = request.Code.Trim();
                var codeExists = await _uow.LocationRepository.FindBy(l => l.Code == code && l.Id != id).AnyAsync();
                if (codeExists)
                    throw new LocationCodeInUseBadRequestException(_localizer);
                location.Code = code;
            }
            if (request.Description != null)
                location.Description = request.Description;
            if (request.WhatsAppContact != null)
                location.WhatsAppContact = request.WhatsAppContact.Trim();
            if (request.PhotoUrl != null)
                location.PhotoUrl = request.PhotoUrl.Trim();
            if (request.Province != null)
                location.Province = request.Province.Trim();
            if (request.Municipality != null)
                location.Municipality = request.Municipality.Trim();
            if (request.Street != null)
                location.Street = request.Street.Trim();

            if (request.BusinessHours != null)
            {
                location.BusinessHoursJson = SerializeBusinessHours(request.BusinessHours);
            }

            if (request.Coordinates != null)
            {
                location.Latitude = request.Coordinates.Lat;
                location.Longitude = request.Coordinates.Lng;
            }

            if (request.IsVerified.HasValue)
                location.IsVerified = request.IsVerified.Value;
            if (request.OffersDelivery.HasValue)
                location.OffersDelivery = request.OffersDelivery.Value;
            if (request.OffersPickup.HasValue)
                location.OffersPickup = request.OffersPickup.Value;

            if (request.BusinessCategoryId != null)
            {
                if (request.BusinessCategoryId.Value <= 0)
                    location.BusinessCategoryId = null;
                else
                {
                    var bc = await _uow.BusinessCategoryRepository.GetAsync(request.BusinessCategoryId.Value);
                    if (bc == null)
                        throw new BusinessCategoryNotFoundException();
                    location.BusinessCategoryId = request.BusinessCategoryId.Value;
                }
            }

            location.ModifiedAt = DateTime.UtcNow;
            _uow.LocationRepository.Update(location);
            await _uow.CommitAsync();
        }

        private static LocationResponse ToResponse(Location location, Organization? organization = null)
        {
            var businessHours = DeserializeBusinessHours(location.BusinessHoursJson);
            var coordinates = (location.Latitude.HasValue && location.Longitude.HasValue)
                ? new PublicLocationCoordinatesDto { Lat = location.Latitude.Value, Lng = location.Longitude.Value }
                : null;
            var isOpenNow = CalculateIsOpenNow(businessHours, DateTime.Now);

            return new LocationResponse
            {
                Id = location.Id,
                OrganizationId = location.OrganizationId,
                OrganizationName = organization?.Name,
                Name = location.Name,
                Code = location.Code,
                Description = location.Description,
                WhatsAppContact = location.WhatsAppContact,
                PhotoUrl = location.PhotoUrl,
                Province = location.Province,
                Municipality = location.Municipality,
                Street = location.Street,
                BusinessHours = businessHours,
                Coordinates = coordinates,
                IsOpenNow = isOpenNow,
                IsVerified = location.IsVerified,
                OffersDelivery = location.OffersDelivery && isOpenNow,
                OffersPickup = location.OffersPickup && isOpenNow,
                BusinessCategoryId = location.BusinessCategoryId,
                BusinessCategory = location.BusinessCategory != null
                    ? new BusinessCategorySummaryDto
                    {
                        Name = location.BusinessCategory.Name,
                        Icon = location.BusinessCategory.Icon
                    }
                    : null,
                CreatedAt = location.CreatedAt,
                ModifiedAt = location.ModifiedAt,
            };
        }

        private static PublicLocationBusinessHoursDto? DeserializeBusinessHours(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<PublicLocationBusinessHoursDto>(json, options);
            }
            catch { return null; }
        }

        private static bool CalculateIsOpenNow(PublicLocationBusinessHoursDto? businessHours, DateTime now)
        {
            if (businessHours == null) return false;
            PublicLocationDayHoursDto? todayHours = now.DayOfWeek switch
            {
                DayOfWeek.Monday => businessHours.Monday,
                DayOfWeek.Tuesday => businessHours.Tuesday,
                DayOfWeek.Wednesday => businessHours.Wednesday,
                DayOfWeek.Thursday => businessHours.Thursday,
                DayOfWeek.Friday => businessHours.Friday,
                DayOfWeek.Saturday => businessHours.Saturday,
                DayOfWeek.Sunday => businessHours.Sunday,
                _ => null
            };
            if (todayHours == null) return false;
            if (!TimeSpan.TryParse(todayHours.Open, out var openTime) || !TimeSpan.TryParse(todayHours.Close, out var closeTime))
                return false;
            var currentTime = now.TimeOfDay;
            return currentTime >= openTime && currentTime <= closeTime;
        }

        private static string? SerializeBusinessHours(PublicLocationBusinessHoursRequest? businessHours)
        {
            if (businessHours == null)
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(businessHours, options);
        }
    }
}
