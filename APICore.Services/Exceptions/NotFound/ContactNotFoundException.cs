using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class ContactNotFoundException : BaseNotFoundException
    {
        public ContactNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404030;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
