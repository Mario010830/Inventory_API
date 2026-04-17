using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class PaymentMethodInactiveBadRequestException : BaseBadRequestException
    {
        public PaymentMethodInactiveBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400468;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
