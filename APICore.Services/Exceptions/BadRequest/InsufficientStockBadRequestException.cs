using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InsufficientStockBadRequestException : BaseBadRequestException
    {
        public InsufficientStockBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400013;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
