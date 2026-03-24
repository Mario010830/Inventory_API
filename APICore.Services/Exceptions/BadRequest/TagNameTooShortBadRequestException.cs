using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class TagNameTooShortBadRequestException : BaseBadRequestException
    {
        public TagNameTooShortBadRequestException(IStringLocalizer<object> localizer)
        {
            CustomCode = 400031;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
