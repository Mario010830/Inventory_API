using APICore.Common.DTO.Request;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
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
    public class PromotionService : IPromotionService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<IPromotionService> _localizer;

        public PromotionService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<IPromotionService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<Promotion> CreatePromotion(CreatePromotionRequest request)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var product = await _uow.ProductRepository.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.OrganizationId == orgId);
            if (product == null)
                throw new ProductNotFoundException(_localizer);

            var type = ParseType(request.Type);
            ValidateBusinessRules(type, request.Value, request.StartsAt, request.EndsAt, request.MinQuantity);

            var promotion = new Promotion
            {
                OrganizationId = orgId,
                ProductId = request.ProductId,
                Type = type,
                Value = request.Value,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                IsActive = request.IsActive,
                MinQuantity = request.MinQuantity
            };

            await _uow.PromotionRepository.AddAsync(promotion);
            await _uow.CommitAsync();
            return promotion;
        }

        public async Task UpdatePromotion(int id, UpdatePromotionRequest request)
        {
            var promotion = await _uow.PromotionRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null)
                throw new BaseNotFoundException("Promocion no encontrada.");

            var type = request.Type != null ? ParseType(request.Type) : promotion.Type;
            var value = request.Value ?? promotion.Value;
            var startsAt = request.StartsAt ?? promotion.StartsAt;
            var endsAt = request.EndsAt ?? promotion.EndsAt;
            var minQuantity = request.MinQuantity ?? promotion.MinQuantity;
            ValidateBusinessRules(type, value, startsAt, endsAt, minQuantity);

            promotion.Type = type;
            promotion.Value = value;
            promotion.StartsAt = startsAt;
            promotion.EndsAt = endsAt;
            promotion.MinQuantity = minQuantity;
            if (request.IsActive.HasValue)
                promotion.IsActive = request.IsActive.Value;

            _uow.PromotionRepository.Update(promotion);
            await _uow.CommitAsync();
        }

        public async Task TogglePromotion(int id, bool isActive)
        {
            var promotion = await _uow.PromotionRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null)
                throw new BaseNotFoundException("Promocion no encontrada.");

            promotion.IsActive = isActive;
            _uow.PromotionRepository.Update(promotion);
            await _uow.CommitAsync();
        }

        public async Task<Promotion> GetPromotion(int id)
        {
            var promotion = await _uow.PromotionRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (promotion == null)
                throw new BaseNotFoundException("Promocion no encontrada.");
            return promotion;
        }

        public async Task<Promotion?> GetActivePromotionForProduct(int productId, decimal quantity, int organizationId)
        {
            var now = DateTime.UtcNow;
            return await _context.Promotions
                .IgnoreQueryFilters()
                .Where(p => p.ProductId == productId
                    && p.OrganizationId == organizationId
                    && p.IsActive
                    && p.MinQuantity <= quantity
                    && (!p.StartsAt.HasValue || p.StartsAt.Value <= now)
                    && (!p.EndsAt.HasValue || p.EndsAt.Value >= now))
                .OrderByDescending(p => p.Value)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PaginatedList<Promotion>> GetPromotions(int? page, int? perPage, int? productId, bool? activeOnly)
        {
            var now = DateTime.UtcNow;
            var query = _uow.PromotionRepository.GetAll();

            if (productId.HasValue)
                query = query.Where(p => p.ProductId == productId.Value);

            if (activeOnly == true)
            {
                query = query.Where(p => p.IsActive
                    && (!p.StartsAt.HasValue || p.StartsAt.Value <= now)
                    && (!p.EndsAt.HasValue || p.EndsAt.Value >= now));
            }

            query = query.OrderByDescending(p => p.CreatedAt);
            return await PaginatedList<Promotion>.CreateAsync(query, page ?? 1, perPage ?? 10);
        }

        private static PromotionType ParseType(string type)
        {
            if (!Enum.TryParse<PromotionType>(type, true, out var parsed))
                throw new BaseBadRequestException("Tipo de promocion invalido. Debe ser percentage o fixed.");
            return parsed;
        }

        private static void ValidateBusinessRules(PromotionType type, decimal value, DateTime? startsAt, DateTime? endsAt, int minQuantity)
        {
            if (minQuantity <= 0)
                throw new BaseBadRequestException("La cantidad minima debe ser mayor que 0.");

            if (value <= 0)
                throw new BaseBadRequestException("El valor de la promocion debe ser mayor que 0.");

            if (type == PromotionType.percentage && (value <= 0 || value >= 100))
                throw new BaseBadRequestException("La promocion porcentual debe estar entre 1 y 99.");

            if (endsAt.HasValue && startsAt.HasValue && endsAt.Value < startsAt.Value)
                throw new BaseBadRequestException("La fecha de fin no puede ser menor que la de inicio.");
        }
    }
}
