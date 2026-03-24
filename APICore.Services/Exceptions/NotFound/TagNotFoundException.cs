using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class TagNotFoundException : BaseNotFoundException
    {
        public TagNotFoundException(IStringLocalizer<object> localizer)
        {
            CustomCode = 404030;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
