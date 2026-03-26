using APICore.API.Authorization;
using APICore.API.BasicResponses;
using APICore.Common.Constants;
using APICore.Common.DTO.Request;
using APICore.Common.DTO.Response;
using APICore.Services;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace APICore.API.Controllers
{
    [Authorize]
    [Route("api/promotion")]
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;
        private readonly IMapper _mapper;

        public PromotionController(IPromotionService promotionService, IMapper mapper)
        {
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [HttpPost]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.Created)]
        public async Task<IActionResult> CreatePromotion([FromBody] CreatePromotionRequest request)
        {
            var result = await _promotionService.CreatePromotion(request);
            var response = _mapper.Map<PromotionResponse>(result);
            return Created("", new ApiCreatedResponse(response));
        }

        [HttpGet]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPromotions(int? page, int? perPage, int? productId, bool? activeOnly)
        {
            var promotions = await _promotionService.GetPromotions(page, perPage, productId, activeOnly);
            var list = _mapper.Map<IEnumerable<PromotionResponse>>(promotions);
            return Ok(new ApiOkPaginatedResponse(list, promotions.GetPaginationData));
        }

        [HttpGet("id")]
        [RequirePermission(PermissionCodes.ProductRead)]
        [ProducesResponseType(typeof(ApiResponse), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPromotionById(int id)
        {
            var promotion = await _promotionService.GetPromotion(id);
            return Ok(new ApiOkResponse(_mapper.Map<PromotionResponse>(promotion)));
        }

        [HttpPut]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] UpdatePromotionRequest request)
        {
            await _promotionService.UpdatePromotion(id, request);
            return NoContent();
        }

        [HttpPatch("{id}/active")]
        [RequirePermission(PermissionCodes.ProductUpdate)]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> TogglePromotion(int id, [FromQuery] bool isActive)
        {
            await _promotionService.TogglePromotion(id, isActive);
            return NoContent();
        }
    }
}
