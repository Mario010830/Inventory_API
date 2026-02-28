using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LeadNotFoundException : BaseNotFoundException
    {
        public LeadNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404031;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
