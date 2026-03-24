using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Common.Helpers;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using APICore.Services.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;

        public SubscriptionService(IUnitOfWork uow, CoreDbContext context)
        {
            _uow = uow ?? throw new ArgumentNullException(nameof(uow));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<Subscription> CreateFreeSubscriptionAsync(int organizationId)
        {
            var org = await _context.Organizations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == organizationId);
            if (org == null)
                throw new OrganizationNotFoundException();

            var freePlan = await _context.Plans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Name == PlanNames.Free && p.IsActive);
            if (freePlan == null)
                throw new PlanNotFoundException();

            var now = DateTime.UtcNow;
            var subscription = new Subscription
            {
                OrganizationId = org.Id,
                PlanId = freePlan.Id,
                BillingCycle = BillingCycle.Monthly,
                Status = SubscriptionStatus.Active,
                StartDate = now,
                EndDate = now.AddMonths(1),
                UpdatedAt = now,
            };
            await _uow.SubscriptionRepository.AddAsync(subscription);
            await _uow.CommitAsync();

            org.SubscriptionId = subscription.Id;
            org.IsActive = true;
            _uow.OrganizationRepository.Update(org);
            await _uow.CommitAsync();

            return subscription;
        }

        public async Task<SubscriptionRequest> CreatePaidSubscriptionRequestAsync(int organizationId, int planId, string billingCycle)
        {
            EnsureValidBillingCycle(billingCycle);

            var plan = await _context.Plans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == planId);
            if (plan == null)
                throw new PlanNotFoundException();
            if (!plan.IsActive)
                throw new InactivePlanBadRequestException();
            if (string.Equals(plan.Name, PlanNames.Free, StringComparison.OrdinalIgnoreCase))
                throw new BaseBadRequestException { CustomCode = 400408, CustomMessage = "El plan gratuito no requiere solicitud de pago." };

            var org = await _context.Organizations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(o => o.Id == organizationId);
            if (org == null)
                throw new OrganizationNotFoundException();

            var now = DateTime.UtcNow;
            var subscription = new Subscription
            {
                OrganizationId = org.Id,
                PlanId = plan.Id,
                BillingCycle = billingCycle.Trim().ToLowerInvariant(),
                Status = SubscriptionStatus.Pending,
                StartDate = now,
                EndDate = now,
                UpdatedAt = now,
            };
            await _uow.SubscriptionRepository.AddAsync(subscription);
            await _uow.CommitAsync();

            var request = new SubscriptionRequest
            {
                SubscriptionId = subscription.Id,
                Type = SubscriptionRequestType.New,
                Status = SubscriptionRequestStatus.Pending,
            };
            await _uow.SubscriptionRequestRepository.AddAsync(request);
            await _uow.CommitAsync();

            org.SubscriptionId = subscription.Id;
            org.IsActive = false;
            _uow.OrganizationRepository.Update(org);
            await _uow.CommitAsync();

            return await _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstAsync(r => r.Id == request.Id);
        }

        public async Task<SubscriptionRequest> ApproveRequestAsync(int requestId, ApproveSubscriptionRequestDto dto, int reviewerUserId)
        {
            var req = await _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Organization)
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null)
                throw new SubscriptionRequestNotFoundException();
            if (req.Status != SubscriptionRequestStatus.Pending)
                throw new SubscriptionRequestInvalidStateBadRequestException();

            var sub = req.Subscription;
            if (sub == null)
                throw new SubscriptionNotFoundException();

            var now = DateTime.UtcNow;
            req.Status = SubscriptionRequestStatus.Approved;
            req.Notes = dto?.Notes;
            req.PaymentReference = dto?.PaymentReference;
            req.ReviewedByUserId = reviewerUserId;
            req.ReviewedAt = now;

            sub.Status = SubscriptionStatus.Active;
            sub.StartDate = now;
            sub.EndDate = ComputeEndDate(now, sub.BillingCycle);
            sub.UpdatedAt = now;
            _uow.SubscriptionRepository.Update(sub);

            var org = sub.Organization;
            if (org != null)
            {
                org.IsActive = true;
                org.SubscriptionId = sub.Id;
                _uow.OrganizationRepository.Update(org);
            }

            _uow.SubscriptionRequestRepository.Update(req);
            await _uow.CommitAsync();

            return await ReloadSubscriptionRequestForMappingAsync(req.Id);
        }

        public async Task<SubscriptionRequest> RejectRequestAsync(int requestId, RejectSubscriptionRequestDto dto, int reviewerUserId)
        {
            var req = await _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Organization)
                .FirstOrDefaultAsync(r => r.Id == requestId);
            if (req == null)
                throw new SubscriptionRequestNotFoundException();
            if (req.Status != SubscriptionRequestStatus.Pending)
                throw new SubscriptionRequestInvalidStateBadRequestException();

            var now = DateTime.UtcNow;
            req.Status = SubscriptionRequestStatus.Rejected;
            req.Notes = dto.Notes;
            req.ReviewedByUserId = reviewerUserId;
            req.ReviewedAt = now;
            _uow.SubscriptionRequestRepository.Update(req);
            await _uow.CommitAsync();

            return await ReloadSubscriptionRequestForMappingAsync(req.Id);
        }

        public async Task<Subscription> RenewSubscriptionAsync(int subscriptionId, RenewSubscriptionRequest dto, int reviewerUserId)
        {
            EnsureValidBillingCycle(dto.BillingCycle);

            var sub = await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Organization)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);
            if (sub == null)
                throw new SubscriptionNotFoundException();

            var cycle = dto.BillingCycle.Trim().ToLowerInvariant();
            var baseDate = sub.EndDate > DateTime.UtcNow ? sub.EndDate : DateTime.UtcNow;
            sub.EndDate = ComputeEndDate(baseDate, cycle);
            sub.BillingCycle = cycle;
            sub.Status = SubscriptionStatus.Active;
            sub.UpdatedAt = DateTime.UtcNow;
            _uow.SubscriptionRepository.Update(sub);

            if (sub.Organization != null)
            {
                sub.Organization.IsActive = true;
                sub.Organization.SubscriptionId = sub.Id;
                _uow.OrganizationRepository.Update(sub.Organization);
            }

            var audit = new SubscriptionRequest
            {
                SubscriptionId = sub.Id,
                Type = SubscriptionRequestType.Renewal,
                Status = SubscriptionRequestStatus.Approved,
                Notes = dto.Notes,
                PaymentReference = dto.PaymentReference,
                ReviewedByUserId = reviewerUserId,
                ReviewedAt = DateTime.UtcNow,
            };
            await _uow.SubscriptionRequestRepository.AddAsync(audit);
            await _uow.CommitAsync();

            return await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Plan)
                .FirstAsync(s => s.Id == sub.Id);
        }

        public async Task<Subscription> ChangePlanAsync(int subscriptionId, ChangePlanRequest dto, int reviewerUserId)
        {
            EnsureValidBillingCycle(dto.BillingCycle);

            var newPlan = await _context.Plans.IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == dto.PlanId);
            if (newPlan == null)
                throw new PlanNotFoundException();
            if (!newPlan.IsActive)
                throw new InactivePlanBadRequestException();

            var sub = await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Organization)
                .FirstOrDefaultAsync(s => s.Id == subscriptionId);
            if (sub == null)
                throw new SubscriptionNotFoundException();

            var cycle = dto.BillingCycle.Trim().ToLowerInvariant();
            var now = DateTime.UtcNow;
            sub.PlanId = newPlan.Id;
            sub.BillingCycle = cycle;
            sub.StartDate = now;
            sub.EndDate = ComputeEndDate(now, cycle);
            sub.Status = SubscriptionStatus.Active;
            sub.UpdatedAt = now;
            _uow.SubscriptionRepository.Update(sub);

            if (sub.Organization != null)
            {
                sub.Organization.IsActive = true;
                sub.Organization.SubscriptionId = sub.Id;
                _uow.OrganizationRepository.Update(sub.Organization);
            }

            var audit = new SubscriptionRequest
            {
                SubscriptionId = sub.Id,
                Type = SubscriptionRequestType.PlanChange,
                Status = SubscriptionRequestStatus.Approved,
                Notes = dto.Notes,
                PaymentReference = dto.PaymentReference,
                ReviewedByUserId = reviewerUserId,
                ReviewedAt = now,
            };
            await _uow.SubscriptionRequestRepository.AddAsync(audit);
            await _uow.CommitAsync();

            return await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Plan)
                .FirstAsync(s => s.Id == sub.Id);
        }

        public async Task CheckAndExpireSubscriptionsAsync()
        {
            var now = DateTime.UtcNow;
            var due = await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Organization)
                .Include(s => s.Plan)
                .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate < now)
                .ToListAsync();

            foreach (var s in due)
            {
                var isFree = s.Plan != null
                    && string.Equals(s.Plan.Name, PlanNames.Free, StringComparison.OrdinalIgnoreCase);

                if (isFree)
                {
                    var end = s.EndDate;
                    while (end < now)
                        end = end.AddMonths(1);
                    s.EndDate = end;
                    s.UpdatedAt = now;
                    _uow.SubscriptionRepository.Update(s);
                    if (s.Organization != null && !s.Organization.IsActive)
                    {
                        s.Organization.IsActive = true;
                        _uow.OrganizationRepository.Update(s.Organization);
                    }
                    continue;
                }

                s.Status = SubscriptionStatus.Expired;
                s.UpdatedAt = now;
                _uow.SubscriptionRepository.Update(s);
                if (s.Organization != null)
                {
                    s.Organization.IsActive = false;
                    _uow.OrganizationRepository.Update(s.Organization);
                }
            }

            if (due.Count > 0)
                await _uow.CommitAsync();
        }

        public async Task<SubscriptionResponse> GetMySubscriptionAsync(int organizationId)
        {
            var org = await _context.Organizations.IgnoreQueryFilters()
                .Include(o => o.Subscription!)
                .ThenInclude(s => s.Plan)
                .Include(o => o.Subscription!)
                .ThenInclude(s => s.Organization)
                .FirstOrDefaultAsync(o => o.Id == organizationId);
            if (org?.Subscription == null)
                throw new SubscriptionNotFoundException();

            return ToSubscriptionResponse(org.Subscription);
        }

        public async Task<PaginatedList<Subscription>> GetAllSubscriptionsAsync(int? page, int? perPage, string statusFilter, int? planId)
        {
            var query = _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Plan)
                .Include(s => s.Organization)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
                query = query.Where(s => s.Status == statusFilter);

            if (planId.HasValue)
                query = query.Where(s => s.PlanId == planId.Value);

            query = query.OrderByDescending(s => s.CreatedAt);

            var p = page.GetValueOrDefault(1);
            var pp = perPage.GetValueOrDefault(10);
            return await PaginatedList<Subscription>.CreateAsync(query, p, pp);
        }

        public async Task<Subscription> GetSubscriptionByIdAsync(int id)
        {
            var sub = await _context.Subscriptions.IgnoreQueryFilters()
                .Include(s => s.Plan)
                .Include(s => s.Organization)
                .FirstOrDefaultAsync(s => s.Id == id);
            if (sub == null)
                throw new SubscriptionNotFoundException();
            return sub;
        }

        public async Task<PaginatedList<SubscriptionRequest>> GetSubscriptionRequestsAsync(int? page, int? perPage, string statusFilter)
        {
            var query = _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Organization)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "all", StringComparison.OrdinalIgnoreCase))
                query = query.Where(r => r.Status == statusFilter);

            query = query.OrderByDescending(r => r.CreatedAt);

            var p = page.GetValueOrDefault(1);
            var pp = perPage.GetValueOrDefault(10);
            return await PaginatedList<SubscriptionRequest>.CreateAsync(query, p, pp);
        }

        public async Task<SubscriptionRequest> GetSubscriptionRequestByIdAsync(int id)
        {
            var req = await _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Organization)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (req == null)
                throw new SubscriptionRequestNotFoundException();
            return req;
        }

        private Task<SubscriptionRequest> ReloadSubscriptionRequestForMappingAsync(int id) =>
            _context.SubscriptionRequests.IgnoreQueryFilters()
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Plan)
                .Include(r => r.Subscription)
                .ThenInclude(s => s.Organization)
                .FirstAsync(r => r.Id == id);

        private static void EnsureValidBillingCycle(string cycle)
        {
            if (string.IsNullOrWhiteSpace(cycle))
                throw new InvalidBillingCycleBadRequestException();
            var c = cycle.Trim().ToLowerInvariant();
            if (c != BillingCycle.Monthly && c != BillingCycle.Annual)
                throw new InvalidBillingCycleBadRequestException();
        }

        private static DateTime ComputeEndDate(DateTime start, string billingCycle)
        {
            return billingCycle.ToLowerInvariant() == BillingCycle.Annual
                ? start.AddYears(1)
                : start.AddMonths(1);
        }

        private static SubscriptionResponse ToSubscriptionResponse(Subscription s)
        {
            var days = SubscriptionDisplayHelper.ComputeDaysRemaining(s.StartDate, s.EndDate, s.BillingCycle, s.Plan?.Name, DateTime.UtcNow);
            return new SubscriptionResponse
            {
                Id = s.Id,
                BillingCycle = s.BillingCycle,
                Status = s.Status,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                DaysRemaining = days,
                Plan = s.Plan == null
                    ? null
                    : new PlanResponse
                    {
                        Id = s.Plan.Id,
                        Name = s.Plan.Name,
                        DisplayName = s.Plan.DisplayName,
                        Description = s.Plan.Description,
                        MaxProducts = s.Plan.MaxProducts,
                        MaxUsers = s.Plan.MaxUsers,
                        MaxLocations = s.Plan.MaxLocations,
                        MonthlyPrice = s.Plan.MonthlyPrice,
                        AnnualPrice = s.Plan.AnnualPrice,
                        IsActive = s.Plan.IsActive,
                    },
                Organization = s.Organization == null
                    ? null
                    : new OrganizationResponse
                    {
                        Id = s.Organization.Id,
                        Name = s.Organization.Name,
                        Code = s.Organization.Code,
                        Description = s.Organization.Description,
                        CreatedAt = s.Organization.CreatedAt,
                        ModifiedAt = s.Organization.ModifiedAt,
                    },
            };
        }
    }
}
