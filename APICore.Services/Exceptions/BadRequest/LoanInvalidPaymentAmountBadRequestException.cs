using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LoanInvalidPaymentAmountBadRequestException : BaseBadRequestException
    {
        public LoanInvalidPaymentAmountBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400092;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
