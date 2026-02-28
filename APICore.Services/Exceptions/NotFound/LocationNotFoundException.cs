using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LocationNotFoundException : BaseNotFoundException
    {
        public LocationNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404021;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
