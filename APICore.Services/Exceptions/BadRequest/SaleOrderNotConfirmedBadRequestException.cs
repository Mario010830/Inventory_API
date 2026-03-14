using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SaleOrderNotConfirmedBadRequestException : BaseBadRequestException
    {
        public SaleOrderNotConfirmedBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400040;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
