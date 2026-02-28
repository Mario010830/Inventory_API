using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ProductCodeInUseBadRequestException : BaseBadRequestException
    {
        public ProductCodeInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400002;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
