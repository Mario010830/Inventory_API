using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class PaymentMethodNameInUseBadRequestException : BaseBadRequestException
    {
        public PaymentMethodNameInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400464;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
