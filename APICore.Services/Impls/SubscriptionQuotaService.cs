using System;
using APICore.Common.Constants;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.Entities.Enums;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SubscriptionQuotaService : ISubscriptionQuotaService
    {
        private readonly CoreDbContext _context;
        private readonly ICurrentUserContextAccessor _currentUserContextAccessor;

        public SubscriptionQuotaService(CoreDbContext context, ICurrentUserContextAccessor currentUserContextAccessor)
        {
            _context = context;
            _currentUserContextAccessor = currentUserContextAccessor ?? throw new ArgumentNullException(nameof(currentUserContextAccessor));
        }

        public async Task EnsureCanAddProductAsync(int organizationId)
        {
            if (_currentUserContextAccessor.GetCurrent()?.IsSuperAdmin == true)
                return;

            var plan = await GetEffectivePlanAsync(organizationId);
            if (plan.MaxProducts < 0)
                return;

            var count = await _context.Products.IgnoreQueryFilters()
                .CountAsync(p => p.OrganizationId == organizationId && !p.IsDeleted);
            if (count >= plan.MaxProducts)
                throw new PlanLimitExceededBadRequestException("Has alcanzado el límite de productos de tu plan actual.");
        }

        public async Task EnsureCanAddUserAsync(int organizationId)
        {
            if (_currentUserContextAccessor.GetCurrent()?.IsSuperAdmin == true)
                return;

            var plan = await GetEffectivePlanAsync(organizationId);
            if (plan.MaxUsers < 0)
                return;

            var count = await _context.Users.IgnoreQueryFilters()
                .CountAsync(u => u.OrganizationId == organizationId && u.Status == StatusEnum.ACTIVE);
            if (count >= plan.MaxUsers)
                throw new PlanLimitExceededBadRequestException("Has alcanzado el límite de usuarios de tu plan actual.");
        }

        public async Task EnsureCanAddLocationAsync(int organizationId)
        {
            if (_currentUserContextAccessor.GetCurrent()?.IsSuperAdmin == true)
                return;

            var plan = await GetEffectivePlanAsync(organizationId);
            if (plan.MaxLocations < 0)
                return;

            var count = await _context.Locations.IgnoreQueryFilters()
                .CountAsync(l => l.OrganizationId == organizationId);
            if (count >= plan.MaxLocations)
                throw new PlanLimitExceededBadRequestException("Has alcanzado el límite de ubicaciones de tu plan actual.");
        }

        private async Task<Plan> GetEffectivePlanAsync(int organizationId)
        {
            var org = await _context.Organizations.IgnoreQueryFilters()
                .AsNoTracking()
                .Include(o => o.Subscription!)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(o => o.Id == organizationId);

            if (org?.Subscription?.Plan != null
                && org.Subscription.Status == SubscriptionStatus.Active)
                return org.Subscription.Plan;

            var free = await _context.Plans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Name.ToLower() == PlanNames.Free);
            if (free == null)
                throw new PlanNotFoundException();
            return free;
        }
    }
}
