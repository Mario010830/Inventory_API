using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class UserAlreadyExistBadRequestException : BaseBadRequestException
    {
        public UserAlreadyExistBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400012;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}