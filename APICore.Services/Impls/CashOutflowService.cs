using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class CashOutflowService : ICashOutflowService
    {
        private const int AmountDecimals = 2;

        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ICashOutflowService> _localizer;

        public CashOutflowService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<ICashOutflowService> localizer)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _localizer = localizer;
        }

        public async Task<CashOutflowResponseDto> CreateAsync(CreateCashOutflowRequest request, int userId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var locationId = _context.CurrentLocationId > 0
                ? _context.CurrentLocationId
                : (request.LocationId ?? 0);
            if (locationId <= 0)
                throw new BaseBadRequestException("Debes indicar la localización (LocationId) para registrar un retiro de caja.");

            var locationOk = await _context.Locations
                .IgnoreQueryFilters()
                .AnyAsync(l => l.Id == locationId && l.OrganizationId == orgId);
            if (!locationOk)
                throw new BaseBadRequestException("La localización indicada no pertenece a tu organización.");

            var businessDate = request.Date.Date;
            if (businessDate > DateTime.UtcNow.Date)
                throw new BaseBadRequestException("La fecha del retiro no puede ser futura.");

            if (await IsDailySummaryClosedAsync(orgId, locationId, businessDate))
                throw new BaseBadRequestException("No se pueden registrar retiros: el cuadre del día ya está cerrado.");

            var amount = Math.Round(request.Amount, AmountDecimals, MidpointRounding.AwayFromZero);
            if (amount <= 0)
                throw new BaseBadRequestException("El importe del retiro debe ser mayor que cero.");

            var entity = new CashOutflow
            {
                OrganizationId = orgId,
                LocationId = locationId,
                Date = businessDate,
                Amount = amount,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                UserId = userId > 0 ? userId : null,
            };
            await _uow.CashOutflowRepository.AddAsync(entity);
            await _uow.CommitAsync();

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var entity = await _uow.CashOutflowRepository.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null || entity.OrganizationId != orgId)
                throw new BaseNotFoundException("Retiro de caja no encontrado.");

            if (await IsDailySummaryClosedAsync(orgId, entity.LocationId, entity.Date))
                throw new BaseBadRequestException("No se puede eliminar el retiro: el cuadre del día ya está cerrado.");

            _uow.CashOutflowRepository.Delete(entity);
            await _uow.CommitAsync();
        }

        public async Task<List<CashOutflowResponseDto>> GetByDateAsync(DateTime date, int? locationId)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var resolvedLocationId = _context.CurrentLocationId > 0
                ? _context.CurrentLocationId
                : (locationId ?? 0);
            if (resolvedLocationId <= 0)
                throw new BaseBadRequestException("Debes indicar la localización (LocationId) para listar retiros.");

            var targetDate = date.Date;
            var list = await _context.CashOutflows
                .Where(c => c.OrganizationId == orgId
                         && c.LocationId == resolvedLocationId
                         && c.Date == targetDate)
                .OrderBy(c => c.Id)
                .ToListAsync();

            return list.Select(MapToDto).ToList();
        }

        private async Task<bool> IsDailySummaryClosedAsync(int organizationId, int locationId, DateTime date)
        {
            var d = date.Date;
            return await _context.DailySummaries
                .IgnoreQueryFilters()
                .AnyAsync(s => s.OrganizationId == organizationId
                            && s.LocationId == locationId
                            && s.Date == d
                            && s.IsClosed);
        }

        private static CashOutflowResponseDto MapToDto(CashOutflow c) =>
            new()
            {
                Id = c.Id,
                Date = c.Date,
                LocationId = c.LocationId,
                Amount = c.Amount,
                Notes = c.Notes,
                UserId = c.UserId,
                CreatedAt = c.CreatedAt,
            };
    }
}
