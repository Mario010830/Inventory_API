using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/loan")]
    public class LoanController : Controller
    {
        private readonly ILoanService _loanService;

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService ?? throw new ArgumentNullException(nameof(loanService));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.LoanCreate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateLoan([FromBody] CreateLoanRequest request)
        {
            var result = await _loanService.CreateLoan(request);
            return Created("", new ApiCreatedResponse(result));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.LoanRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetLoans(int? page, int? perPage)
        {
            var loans = await _loanService.GetLoans(page, perPage);
            return Ok(new ApiOkPaginatedResponse(loans, loans.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.LoanRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetLoanById(int id)
        {
            var loan = await _loanService.GetLoan(id);
            return Ok(new ApiOkResponse(loan));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.LoanUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> UpdateLoan(int id, [FromBody] UpdateLoanRequest request)
        {
            await _loanService.UpdateLoan(id, request);
            return NoContent();
        }

        [HttpDelete]
        [RequirePermission(PermissionCodes.LoanDelete)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> DeleteLoan(int id)
        {
            await _loanService.DeleteLoan(id);
            return NoContent();
        }

        [HttpPost("{id}/payments")]
        [RequirePermission(PermissionCodes.LoanUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> RegisterPayment(int id, [FromBody] RegisterLoanPaymentRequest request)
        {
            var result = await _loanService.RegisterPayment(id, request);
            return Ok(new ApiOkResponse(result));
        }
    }
}
