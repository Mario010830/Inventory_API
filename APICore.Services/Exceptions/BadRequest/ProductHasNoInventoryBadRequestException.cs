using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductHasNoInventoryBadRequestException : BaseBadRequestException
    {
        public ProductHasNoInventoryBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400031;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
