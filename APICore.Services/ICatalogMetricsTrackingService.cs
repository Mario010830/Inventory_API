using APICore.Common.DTO.Request;
using APICore.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ICatalogMetricsTrackingService
    {
        Task AppendPublicEventsAsync(CatalogMetricsBatchRequest request, int? authenticatedUserId, CancellationToken cancellationToken = default);

        /// <summary>Encola eventos de compra en el contexto EF sin guardar; llamar antes de CommitAsync de la unidad de trabajo.</summary>
        void StagePurchaseCompletedEvents(SaleOrder order);
    }
}