using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SupplierService : ISupplierService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ISupplierService> _localizer;

        public SupplierService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<ISupplierService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<Supplier> CreateSupplier(CreateSupplierRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var nameExists = await _uow.SupplierRepository.FindAllAsync(s => s.Name == request.Name && s.OrganizationId == orgId);
            if (nameExists != null && nameExists.Count > 0)
            {
                throw new SupplierNameInUseBadRequestException(_localizer);
            }

            var newSupplier = new Supplier
            {
                OrganizationId = orgId,
                Name = request.Name,
                ContactPerson = request.ContactPerson,
                Phone = request.Phone,
                Email = request.Email,
                Address = request.Address,
                Notes = request.Notes,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };

            await _uow.SupplierRepository.AddAsync(newSupplier);
            await _uow.CommitAsync();

            return newSupplier;
        }

        public async Task DeleteSupplier(int id)
        {
            var supplier = await _uow.SupplierRepository.FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null)
            {
                throw new SupplierNotFoundException(_localizer);
            }

            var movements = await _uow.InventoryMovementRepository.FindAllAsync(m => m.SupplierId == id);
            if (movements != null && movements.Count > 0)
            {
                throw new SupplierHasMovementsBadRequestException(_localizer);
            }

            _uow.SupplierRepository.Delete(supplier);
            await _uow.CommitAsync();
        }

        public async Task<PaginatedList<Supplier>> GetAllSuppliers(int? page, int? perPage, string sortOrder = null)
        {
            var suppliers = _uow.SupplierRepository.GetAll();
            var pageIndex = page ?? 1;
            var perPageIndex = perPage ?? 10;
            return await PaginatedList<Supplier>.CreateAsync(suppliers, pageIndex, perPageIndex);
        }

        public async Task<Supplier> GetSupplier(int id)
        {
            var supplier = await _uow.SupplierRepository.FirstOrDefaultAsync(s => s.Id == id);
            if (supplier == null)
            {
                throw new SupplierNotFoundException(_localizer);
            }
            return supplier;
        }

        public async Task UpdateSupplier(int id, UpdateSupplierRequest request)
        {
            var oldSupplier = await _uow.SupplierRepository.FirstOrDefaultAsync(s => s.Id == id);
            if (oldSupplier == null)
            {
                throw new SupplierNotFoundException(_localizer);
            }

            if (request.Name != null)
            {
                var orgId = _context.CurrentOrganizationId;
                var nameExists = await _uow.SupplierRepository.FindAllAsync(s => s.Name == request.Name && s.Id != id && s.OrganizationId == orgId);
                if (nameExists != null && nameExists.Count > 0)
                {
                    throw new SupplierNameInUseBadRequestException(_localizer);
                }
            }

            var updatedSupplier = new Supplier
            {
                Id = oldSupplier.Id,
                OrganizationId = oldSupplier.OrganizationId,
                CreatedAt = oldSupplier.CreatedAt,
                ModifiedAt = DateTime.UtcNow,
                Name = request.Name ?? oldSupplier.Name,
                ContactPerson = request.ContactPerson ?? oldSupplier.ContactPerson,
                Phone = request.Phone ?? oldSupplier.Phone,
                Email = request.Email ?? oldSupplier.Email,
                Address = request.Address ?? oldSupplier.Address,
                Notes = request.Notes ?? oldSupplier.Notes,
                IsActive = request.IsActive ?? oldSupplier.IsActive,
            };

            await _uow.SupplierRepository.UpdateAsync(updatedSupplier, oldSupplier.Id);
            await _uow.CommitAsync();
        }
    }
}
