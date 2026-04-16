using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services.Utils;
using System.Threading.Tasks;

namespace APICore.Services
{
    public interface ILoanService
    {
        Task<LoanResponse> CreateLoan(CreateLoanRequest request);
        Task UpdateLoan(int id, UpdateLoanRequest request);
        Task DeleteLoan(int id);
        Task<LoanResponse> GetLoan(int id);
        Task<PaginatedList<LoanResponse>> GetLoans(int? page, int? perPage);
        Task<LoanResponse> RegisterPayment(int loanId, RegisterLoanPaymentRequest request);
    }
}
