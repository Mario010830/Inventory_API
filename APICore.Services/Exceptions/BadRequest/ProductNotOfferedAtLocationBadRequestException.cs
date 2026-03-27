using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductNotOfferedAtLocationBadRequestException : BaseBadRequestException
    {
        public ProductNotOfferedAtLocationBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400044;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
