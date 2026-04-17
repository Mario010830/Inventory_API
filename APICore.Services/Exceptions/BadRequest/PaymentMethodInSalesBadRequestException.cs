using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class PaymentMethodInSalesBadRequestException : BaseBadRequestException
    {
        public PaymentMethodInSalesBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400465;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
