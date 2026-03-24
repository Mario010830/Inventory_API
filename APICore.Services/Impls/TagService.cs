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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class TagService : ITagService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<ITagService> _localizer;

        public TagService(IUnitOfWork uow, CoreDbContext context, IStringLocalizer<ITagService> localizer)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
        }

        public async Task<IEnumerable<TagResponse>> GetAllAsync()
        {
            var tags = await _uow.TagRepository.GetAllAsync();
            var tagIds = tags.Select(t => t.Id).ToList();
            if (tagIds.Count == 0)
                return Array.Empty<TagResponse>();

            var counts = await _context.ProductTags
                .Where(pt => tagIds.Contains(pt.TagId))
                .GroupBy(pt => pt.TagId)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            return tags.Select(t => new TagResponse
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Color = t.Color,
                ProductCount = counts.TryGetValue(t.Id, out var c) ? c : 0,
                CreatedAt = t.CreatedAt
            }).ToList();
        }

        public async Task<TagResponse> GetByIdAsync(int id)
        {
            var tag = await _uow.TagRepository.GetAsync(id);
            if (tag == null)
                throw new TagNotFoundException(_localizer);

            var productCount = await _context.ProductTags.CountAsync(pt => pt.TagId == id);

            return new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Color = tag.Color,
                ProductCount = productCount,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<TagResponse> CreateAsync(CreateTagRequest request)
        {
            var name = (request.Name ?? "").Trim();
            if (name.Length < 2)
                throw new TagNameTooShortBadRequestException(_localizer);

            var slug = GenerateSlug(name);
            var color = string.IsNullOrWhiteSpace(request.Color) ? "#6366f1" : request.Color.Trim();

            var existsByName = await _uow.TagRepository.FirstOrDefaultAsync(t => t.Name == name);
            if (existsByName != null)
                throw new TagConflictException("Ya existe una etiqueta con ese nombre.");

            var existsBySlug = await _uow.TagRepository.FirstOrDefaultAsync(t => t.Slug == slug);
            if (existsBySlug != null)
                throw new TagConflictException("Ya existe una etiqueta con ese slug.");

            var tag = new Tag
            {
                Name = name,
                Slug = slug,
                Color = color,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.TagRepository.AddAsync(tag);
            await _uow.CommitAsync();

            return new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Color = tag.Color,
                ProductCount = 0,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task<TagResponse> UpdateAsync(int id, UpdateTagRequest request)
        {
            var tag = await _uow.TagRepository.GetAsync(id);
            if (tag == null)
                throw new TagNotFoundException(_localizer);

            if (request.Name != null)
            {
                var name = request.Name.Trim();
                if (name.Length < 2)
                    throw new TagNameTooShortBadRequestException(_localizer);

                var slug = GenerateSlug(name);
                var existsByName = await _uow.TagRepository.FirstOrDefaultAsync(t => t.Name == name && t.Id != id);
                if (existsByName != null)
                    throw new TagConflictException("Ya existe una etiqueta con ese nombre.");

                var existsBySlug = await _uow.TagRepository.FirstOrDefaultAsync(t => t.Slug == slug && t.Id != id);
                if (existsBySlug != null)
                    throw new TagConflictException("Ya existe una etiqueta con ese slug.");

                tag.Name = name;
                tag.Slug = slug;
            }

            if (request.Color != null)
                tag.Color = request.Color.Trim();

            _uow.TagRepository.Update(tag);
            await _uow.CommitAsync();

            var productCount = await _context.ProductTags.CountAsync(pt => pt.TagId == id);

            return new TagResponse
            {
                Id = tag.Id,
                Name = tag.Name,
                Slug = tag.Slug,
                Color = tag.Color,
                ProductCount = productCount,
                CreatedAt = tag.CreatedAt
            };
        }

        public async Task DeleteAsync(int id)
        {
            var tag = await _uow.TagRepository.GetAsync(id);
            if (tag == null)
                throw new TagNotFoundException(_localizer);

            var productCount = await _context.ProductTags.CountAsync(pt => pt.TagId == id);

            if (productCount > 0)
                throw new TagHasProductsBadRequestException(
                    $"Esta etiqueta tiene {productCount} productos asignados. Desasignarla antes de eliminar.");

            _uow.TagRepository.Delete(tag);
            await _uow.CommitAsync();
        }

        internal static string GenerateSlug(string name)
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
