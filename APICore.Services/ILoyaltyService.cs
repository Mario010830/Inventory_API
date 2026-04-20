using APICore.Common.DTO.Response;
using APICore.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ILoyaltyService
    {
        Task ProcessConfirmedSaleOrderAsync(SaleOrder order, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revierte la lealtad otorgada al confirmar el pedido cuando el pedido queda en devolución total (estado returned).
        /// Idempotente por pedido.
        /// </summary>
        Task ProcessFullyReturnedSaleOrderAsync(SaleOrder order, CancellationToken cancellationToken = default);

        Task<CustomerLoyaltyAccountResponse> GetLoyaltySummaryForContactAsync(int contactId, CancellationToken cancellationToken = default);
    }
}
