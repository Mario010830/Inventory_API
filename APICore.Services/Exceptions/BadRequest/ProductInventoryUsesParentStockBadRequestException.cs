using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductInventoryUsesParentStockBadRequestException : BaseBadRequestException
    {
        public ProductInventoryUsesParentStockBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400516;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
