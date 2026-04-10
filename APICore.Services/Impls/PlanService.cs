using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class PlanService : IPlanService
    {
        private readonly IUnitOfWork _uow;

        public PlanService(IUnitOfWork uow)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        }

        public async Task<IReadOnlyList<Plan>> GetActivePlansAsync()
        {
            return await _uow.PlanRepository.GetAll()
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.MonthlyPrice)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Plan> GetByIdAsync(int id)
        {
            var plan = await _uow.PlanRepository.FirstOrDefaultAsync(p => p.Id == id);
            if (plan == null)
                throw new PlanNotFoundException();
            return plan;
        }

        public async Task<Plan> CreateAsync(CreateOrUpdatePlanRequest request)
        {
            var nameTaken = await _uow.PlanRepository.FindBy(p => p.Name == request.Name.Trim()).AnyAsync();
            if (nameTaken)
                throw new BaseBadRequestException { CustomCode = 400406, CustomMessage = "Ya existe un plan con ese nombre." };

            var plan = new Plan
            {
                Name = request.Name.Trim(),
                DisplayName = request.DisplayName.Trim(),
                Description = request.Description?.Trim(),
                MaxProducts = request.MaxProducts,
                MaxUsers = request.MaxUsers,
                MaxLocations = request.MaxLocations,
                MonthlyPrice = request.MonthlyPrice,
                AnnualPrice = request.AnnualPrice,
                IsActive = request.IsActive,
            };
            await _uow.PlanRepository.AddAsync(plan);
            await _uow.CommitAsync();
            return plan;
        }

        public async Task UpdateAsync(int id, CreateOrUpdatePlanRequest request)
        {
            var plan = await GetByIdAsync(id);
            var nameTaken = await _uow.PlanRepository.FindBy(p => p.Name == request.Name.Trim() && p.Id != id).AnyAsync();
            if (nameTaken)
                throw new BaseBadRequestException { CustomCode = 400406, CustomMessage = "Ya existe un plan con ese nombre." };

            plan.Name = request.Name.Trim();
            plan.DisplayName = request.DisplayName.Trim();
            plan.Description = request.Description?.Trim();
            plan.MaxProducts = request.MaxProducts;
            plan.MaxUsers = request.MaxUsers;
            plan.MaxLocations = request.MaxLocations;
            plan.MonthlyPrice = request.MonthlyPrice;
            plan.AnnualPrice = request.AnnualPrice;
            plan.IsActive = request.IsActive;
            _uow.PlanRepository.Update(plan);
            await _uow.CommitAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var plan = await GetByIdAsync(id);
            if (string.Equals(plan.Name, PlanNames.Free, StringComparison.OrdinalIgnoreCase)
                || string.Equals(plan.Name, PlanNames.Pro, StringComparison.OrdinalIgnoreCase)
                || string.Equals(plan.Name, PlanNames.Enterprise, StringComparison.OrdinalIgnoreCase))
                throw new BaseBadRequestException { CustomCode = 400407, CustomMessage = "No se pueden eliminar los planes base del sistema." };

            var inUse = await _uow.SubscriptionRepository.FindBy(s => s.PlanId == id).AnyAsync();
            if (inUse)
                throw new PlanInUseCannotDeleteBadRequestException();

            _uow.PlanRepository.Delete(plan);
            await _uow.CommitAsync();
        }
    }
}
