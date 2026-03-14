using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleOrderAlreadyCancelledBadRequestException : BaseBadRequestException
    {
        public SaleOrderAlreadyCancelledBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400041;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
