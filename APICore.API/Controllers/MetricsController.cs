using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    /// <summary>
    /// Métricas de catálogo público por organización (businessId = organizationId).
    /// </summary>
    [Authorize]
    [Route("api/metrics")]
    public class MetricsController : Controller
    {
        private readonly IMetricsService _metricsService;

        public MetricsController(IMetricsService metricsService)
        {
            _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        }

        [HttpGet("{businessId:int}/traffic")]
        [RequirePermission(PermissionCodes.MetricsRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Forbidden)]
        public async Task<IActionResult> GetTraffic(int businessId, [FromQuery] string period = "30d")
        {
            var result = await _metricsService.GetTrafficAsync(businessId, period);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("{businessId:int}/products")]
        [RequirePermission(PermissionCodes.MetricsRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetProducts(int businessId, [FromQuery] string period = "30d")
        {
            var result = await _metricsService.GetProductsAsync(businessId, period);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("{businessId:int}/sales")]
        [RequirePermission(PermissionCodes.MetricsRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetSales(int businessId, [FromQuery] string period = "30d")
        {
            var result = await _metricsService.GetSalesAsync(businessId, period);
            return Ok(new ApiOkResponse(result));
        }

        [HttpGet("{businessId:int}/customers")]
        [RequirePermission(PermissionCodes.MetricsRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetCustomers(int businessId, [FromQuery] string period = "30d")
        {
            var result = await _metricsService.GetCustomersAsync(businessId, period);
            return Ok(new ApiOkResponse(result));
        }
    }
}
