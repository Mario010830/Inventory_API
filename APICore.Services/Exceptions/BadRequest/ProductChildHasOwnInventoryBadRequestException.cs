using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductChildHasOwnInventoryBadRequestException : BaseBadRequestException
    {
        public ProductChildHasOwnInventoryBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400517;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
