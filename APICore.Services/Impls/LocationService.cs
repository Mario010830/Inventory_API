using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
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
    public class LocationService : ILocationService
    {
        private readonly IUnitOfWork _uow;
        private readonly IStringLocalizer<ILocationService> _localizer;

        public LocationService(IUnitOfWork uow, IStringLocalizer<ILocationService> localizer)
        {
            _uow = uow;
            _localizer = localizer;
        }

        public async Task<LocationResponse> CreateLocation(CreateLocationRequest request)
        {
            var organization = await _uow.OrganizationRepository.FirstOrDefaultAsync(o => o.Id == request.OrganizationId);
            if (organization == null)
                throw new OrganizationNotFoundException(_localizer);

            var codeExists = await _uow.LocationRepository.FindBy(l => l.Code == request.Code).AnyAsync();
            if (codeExists)
                throw new LocationCodeInUseBadRequestException(_localizer);

            var location = new Location
            {
                OrganizationId = request.OrganizationId,
                Name = request.Name,
                Code = request.Code.Trim(),
                Description = request.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.LocationRepository.AddAsync(location);
            await _uow.CommitAsync();
            return ToResponse(location, organization);
        }

        public async Task DeleteLocation(int id)
        {
            var location = await _uow.LocationRepository.FirstOrDefaultAsync(l => l.Id == id);
            if (location == null)
                throw new LocationNotFoundException(_localizer);

            var hasUsers = await _uow.UserRepository.FindBy(u => u.LocationId == id).AnyAsync();
            var hasInventories = await _uow.InventoryRepository.FindBy(i => i.LocationId == id).AnyAsync();
            var hasMovements = await _uow.InventoryMovementRepository.FindBy(m => m.LocationId == id).AnyAsync();
            if (hasUsers || hasInventories || hasMovements)
                throw new LocationInUseCannotDeleteBadRequestException(_localizer);

            _uow.LocationRepository.Delete(location);
            await _uow.CommitAsync();
        }

        public async Task<LocationResponse> GetLocation(int id)
        {
            var location = await _uow.LocationRepository.FindBy(l => l.Id == id)
                .Include(l => l.Organization)
                .FirstOrDefaultAsync();
            if (location == null)
                throw new LocationNotFoundException(_localizer);
            return ToResponse(location, location.Organization);
        }

        public async Task<PaginatedList<LocationResponse>> GetAllLocations(int? page, int? perPage, int? organizationId = null, string sortOrder = null)
        {
            var query = _uow.LocationRepository.GetAllIncluding(l => l.Organization).AsQueryable();
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

            location.ModifiedAt = DateTime.UtcNow;
            _uow.LocationRepository.Update(location);
            await _uow.CommitAsync();
        }

        private static LocationResponse ToResponse(Location location, Organization? organization = null)
        {
            return new LocationResponse
            {
                Id = location.Id,
                OrganizationId = location.OrganizationId,
                OrganizationName = organization?.Name,
                Name = location.Name,
                Code = location.Code,
                Description = location.Description,
                CreatedAt = location.CreatedAt,
                ModifiedAt = location.ModifiedAt,
            };
        }
    }
}
