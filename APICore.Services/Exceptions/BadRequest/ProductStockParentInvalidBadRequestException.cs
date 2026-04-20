using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductStockParentInvalidBadRequestException : BaseBadRequestException
    {
        public ProductStockParentInvalidBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400515;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
