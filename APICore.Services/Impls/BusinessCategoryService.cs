using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class BusinessCategoryService : IBusinessCategoryService
    {
        private readonly IUnitOfWork _uow;

        public BusinessCategoryService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<IEnumerable<BusinessCategoryResponse>> GetActiveAsync()
        {
            var list = await _uow.BusinessCategoryRepository.GetAll()
                .AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.SortOrder)
                .ThenBy(b => b.Name)
                .ToListAsync();
            return list.Select(Map).ToList();
        }

        public async Task<BusinessCategoryResponse> GetByIdAsync(int id)
        {
            var entity = await _uow.BusinessCategoryRepository.FirstOrDefaultAsync(b => b.Id == id);
            if (entity == null)
                throw new BusinessCategoryNotFoundException();
            return Map(entity);
        }

        public async Task<BusinessCategoryResponse> CreateAsync(CreateBusinessCategoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var name = (request.Name ?? "").Trim();
            if (name.Length < 2)
                throw new BaseBadRequestException { CustomCode = 400460, CustomMessage = "El nombre debe tener al menos 2 caracteres." };

            var slug = GenerateSlug(name);
            if (string.IsNullOrEmpty(slug))
                throw new BaseBadRequestException { CustomCode = 400461, CustomMessage = "No se pudo generar un slug válido a partir del nombre." };

            var slugTaken = await _uow.BusinessCategoryRepository.FirstOrDefaultAsync(b => b.Slug == slug);
            if (slugTaken != null)
                throw new BusinessCategorySlugInUseBadRequestException("Ya existe una categoría con ese slug.");

            var icon = string.IsNullOrWhiteSpace(request.Icon) ? "store" : request.Icon.Trim();

            var entity = new BusinessCategory
            {
                Name = name,
                Slug = slug,
                Icon = icon,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder
            };
            await _uow.BusinessCategoryRepository.AddAsync(entity);
            await _uow.CommitAsync();
            return Map(entity);
        }

        public async Task<BusinessCategoryResponse> UpdateAsync(int id, UpdateBusinessCategoryRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var entity = await _uow.BusinessCategoryRepository.GetAsync(id);
            if (entity == null)
                throw new BusinessCategoryNotFoundException();

            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (name.Length < 2)
                    throw new BaseBadRequestException { CustomCode = 400460, CustomMessage = "El nombre debe tener al menos 2 caracteres." };

                var slug = GenerateSlug(name);
                if (string.IsNullOrEmpty(slug))
                    throw new BaseBadRequestException { CustomCode = 400461, CustomMessage = "No se pudo generar un slug válido a partir del nombre." };

                var slugTaken = await _uow.BusinessCategoryRepository.FirstOrDefaultAsync(b => b.Slug == slug && b.Id != id);
                if (slugTaken != null)
                    throw new BusinessCategorySlugInUseBadRequestException("Ya existe una categoría con ese slug.");

                entity.Name = name;
                entity.Slug = slug;
            }

            if (request.Icon != null)
                entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? "store" : request.Icon.Trim();

            if (request.IsActive.HasValue)
                entity.IsActive = request.IsActive.Value;

            if (request.SortOrder.HasValue)
                entity.SortOrder = request.SortOrder.Value;

            _uow.BusinessCategoryRepository.Update(entity);
            await _uow.CommitAsync();
            return Map(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _uow.BusinessCategoryRepository.GetAsync(id);
            if (entity == null)
                throw new BusinessCategoryNotFoundException();

            var count = await _uow.LocationRepository.FindBy(l => l.BusinessCategoryId == id).CountAsync();
            if (count > 0)
                throw new BusinessCategoryHasLocationsBadRequestException(
                    $"Hay {count} local(es) usando esta categoría. Reasígnelos antes de eliminar.");

            _uow.BusinessCategoryRepository.Delete(entity);
            await _uow.CommitAsync();
        }

        private static BusinessCategoryResponse Map(BusinessCategory b)
        {
            return new BusinessCategoryResponse
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                Icon = b.Icon,
                IsActive = b.IsActive,
                SortOrder = b.SortOrder,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.ModifiedAt
            };
        }

        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            var normalized = name.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var slug = sb.ToString().Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Trim();
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"[^a-z0-9-]", "");
            slug = Regex.Replace(slug, @"-+", "-").Trim('-');
            return slug;
        }
    }
}
