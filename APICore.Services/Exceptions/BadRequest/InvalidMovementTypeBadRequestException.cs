using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class InvalidMovementTypeBadRequestException : BaseBadRequestException
    {
        public InvalidMovementTypeBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400006;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }
    }
}
