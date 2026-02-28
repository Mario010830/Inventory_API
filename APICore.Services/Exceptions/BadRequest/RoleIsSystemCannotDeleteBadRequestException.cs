using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class RoleIsSystemCannotDeleteBadRequestException : BaseBadRequestException
    {
        public RoleIsSystemCannotDeleteBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400020;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
