using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LoanInvalidPrincipalBadRequestException : BaseBadRequestException
    {
        public LoanInvalidPrincipalBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400091;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
