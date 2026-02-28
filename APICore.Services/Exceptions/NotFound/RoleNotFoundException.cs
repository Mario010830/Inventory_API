using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class RoleNotFoundException : BaseNotFoundException
    {
        public RoleNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404020;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
