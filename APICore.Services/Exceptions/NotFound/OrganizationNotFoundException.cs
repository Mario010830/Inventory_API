using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class OrganizationNotFoundException : BaseNotFoundException
    {
        public OrganizationNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404022;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
