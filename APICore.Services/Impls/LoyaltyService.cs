using APICore.Common.DTO.Response;
using APICore.Data;
using APICore.Data.Entities;
using APICore.Data.UoW;
using APICore.Services.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services.Impls
{
    public class LoyaltyService : ILoyaltyService
    {
        private const string LoyaltyNoteMilestone = "loyalty_milestone";
        private const string LoyaltyNoteReversal = "loyalty_reversal";

        private readonly IUnitOfWork _uow;
        private readonly CoreDbContext _context;
        private readonly IStringLocalizer<object> _localizer;
        private readonly ILoyaltyThresholdNotifier _notifier;

        public LoyaltyService(
            IUnitOfWork uow,
            CoreDbContext context,
            IStringLocalizer<object> localizer,
            ILoyaltyThresholdNotifier notifier)
        {
            _uow = uow;
            _context = context;
            _localizer = localizer;
            _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        }

        public async Task<CustomerLoyaltyAccountResponse> GetLoyaltySummaryForContactAsync(int contactId, CancellationToken cancellationToken = default)
        {
            var orgId = _context.CurrentOrganizationId;
            if (orgId <= 0)
                throw new UnauthorizedException(_localizer);

            var contact = await _uow.ContactRepository.FirstOrDefaultAsync(c => c.Id == contactId);
            if (contact == null)
                throw new ContactNotFoundException(_localizer);

            var settings = await GetOrCreateSettingsAsync(orgId, cancellationToken);
            var account = await _uow.CustomerLoyaltyAccountRepository
                .FirstOrDefaultAsync(a => a.OrganizationId == orgId && a.ContactId == contactId);

            var lifetime = account?.LifetimeOrders ?? 0;
            var n = Math.Max(1, settings.NotifyEveryNOrders);
            var remainder = lifetime % n;
            var until = remainder == 0 && lifetime > 0 ? 0 : (lifetime == 0 ? n : n - remainder);

            var response = new CustomerLoyaltyAccountResponse
            {
                ContactId = contactId,
                PointsBalance = account?.PointsBalance ?? 0,
                LifetimeOrders = lifetime,
                LastPurchaseAt = account?.LastPurchaseAt,
                NotifyEveryNOrders = settings.NotifyEveryNOrders,
                OrdersUntilNextMilestone = until,
            };

            await _uow.CommitAsync();
            return response;
        }

        public async Task ProcessConfirmedSaleOrderAsync(SaleOrder order, CancellationToken cancellationToken = default)
        {
            if (!order.ContactId.HasValue)
                return;

            var orgId = order.OrganizationId;
            var contactId = order.ContactId.Value;

            var settings = await GetOrCreateSettingsAsync(orgId, cancellationToken);
            var points = Math.Max(0, settings.PointsPerOrder);

            var account = await _uow.CustomerLoyaltyAccountRepository
                .FirstOrDefaultAsync(a => a.OrganizationId == orgId && a.ContactId == contactId);

            if (account == null)
            {
                account = new CustomerLoyaltyAccount
                {
                    OrganizationId = orgId,
                    ContactId = contactId,
                    PointsBalance = 0,
                    LifetimeOrders = 0,
                    LastPurchaseAt = null,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                };
                await _uow.CustomerLoyaltyAccountRepository.AddAsync(account);
            }

            account.LifetimeOrders += 1;
            account.PointsBalance += points;
            account.LastPurchaseAt = DateTime.UtcNow;
            account.ModifiedAt = DateTime.UtcNow;
            _uow.CustomerLoyaltyAccountRepository.Update(account);

            var earn = new LoyaltyEvent
            {
                OrganizationId = orgId,
                ContactId = contactId,
                SaleOrderId = order.Id,
                PointsDelta = points,
                OccurredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.LoyaltyEventRepository.AddAsync(earn);

            var n = settings.NotifyEveryNOrders;
            if (n > 0 && account.LifetimeOrders % n == 0)
            {
                var milestone = new LoyaltyEvent
                {
                    OrganizationId = orgId,
                    ContactId = contactId,
                    SaleOrderId = order.Id,
                    PointsDelta = 0,
                    OccurredAt = DateTime.UtcNow,
                    Note = LoyaltyNoteMilestone,
                    CreatedAt = DateTime.UtcNow,
                    ModifiedAt = DateTime.UtcNow,
                };
                await _uow.LoyaltyEventRepository.AddAsync(milestone);

                await _notifier.NotifyLoyaltyMilestoneAsync(orgId, order.LocationId, contactId, account.LifetimeOrders, cancellationToken);
            }
        }

        public async Task ProcessFullyReturnedSaleOrderAsync(SaleOrder order, CancellationToken cancellationToken = default)
        {
            if (order == null)
                return;

            var reversalExists = await _uow.LoyaltyEventRepository
                .GetAll()
                .AnyAsync(e => e.SaleOrderId == order.Id && e.Note == LoyaltyNoteReversal, cancellationToken);
            if (reversalExists)
                return;

            var earnRows = await _uow.LoyaltyEventRepository
                .GetAll()
                .Where(e => e.SaleOrderId == order.Id && (e.Note == null || e.Note == ""))
                .ToListAsync(cancellationToken);

            if (earnRows.Count == 0)
                return;

            var pointsEarned = earnRows.Sum(e => e.PointsDelta);
            if (pointsEarned < 0)
                pointsEarned = 0;

            var orgId = order.OrganizationId;
            var contactId = order.ContactId ?? earnRows[0].ContactId;
            if (contactId <= 0)
                return;

            var account = await _uow.CustomerLoyaltyAccountRepository
                .FirstOrDefaultAsync(a => a.OrganizationId == orgId && a.ContactId == contactId);

            var lifetimeDelta = earnRows.Count;

            if (account != null)
            {
                account.LifetimeOrders = Math.Max(0, account.LifetimeOrders - lifetimeDelta);
                account.PointsBalance = Math.Max(0, account.PointsBalance - pointsEarned);
                account.ModifiedAt = DateTime.UtcNow;
                _uow.CustomerLoyaltyAccountRepository.Update(account);
            }

            var reversal = new LoyaltyEvent
            {
                OrganizationId = orgId,
                ContactId = contactId,
                SaleOrderId = order.Id,
                PointsDelta = -pointsEarned,
                OccurredAt = DateTime.UtcNow,
                Note = LoyaltyNoteReversal,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.LoyaltyEventRepository.AddAsync(reversal);
        }

        private async Task<LoyaltySettings> GetOrCreateSettingsAsync(int organizationId, CancellationToken cancellationToken)
        {
            var existing = await _uow.LoyaltySettingsRepository
                .FirstOrDefaultAsync(s => s.OrganizationId == organizationId);
            if (existing != null)
                return existing;

            var created = new LoyaltySettings
            {
                OrganizationId = organizationId,
                PointsPerOrder = 1,
                NotifyEveryNOrders = 5,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
            };
            await _uow.LoyaltySettingsRepository.AddAsync(created);
            return created;
        }
    }
}
