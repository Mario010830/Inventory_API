using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services
{
    /// <summary>
    /// Aviso opcional al alcanzar umbral de lealtad (p. ej. push a la ubicación).
    /// </summary>
    public interface ILoyaltyThresholdNotifier
    {
        Task NotifyLoyaltyMilestoneAsync(int organizationId, int locationId, int contactId, int lifetimeOrders, CancellationToken cancellationToken = default);
    }
}
