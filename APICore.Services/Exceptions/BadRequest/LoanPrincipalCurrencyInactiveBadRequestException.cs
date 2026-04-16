using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LoanPrincipalCurrencyInactiveBadRequestException : BaseBadRequestException
    {
        public LoanPrincipalCurrencyInactiveBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400093;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
