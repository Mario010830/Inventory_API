using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class OrganizationCodeInUseBadRequestException : BaseBadRequestException
    {
        public OrganizationCodeInUseBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400027;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
