using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class LoanNotFoundException : BaseNotFoundException
    {
        public LoanNotFoundException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 404044;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
