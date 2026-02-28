using System.Net;
using Microsoft.Extensions.Localization;

namespace APICore.Services.Exceptions
{
    public class SendEmailBadRequestException : BaseBadRequestException
    {
        public SendEmailBadRequestException(IStringLocalizer<object> localizer) : base()
        {
            CustomCode = 400008;
            CustomMessage = localizer.GetString(CustomCode.ToString());
        }

        public SendEmailBadRequestException(IStringLocalizer<object> localizer, System.Net.HttpStatusCode statusCode, string sendGridBody) : base()
        {
            CustomCode = 400008;
            var baseMessage = localizer.GetString(CustomCode.ToString());
            CustomMessage = $"{baseMessage} SendGrid ({(int)statusCode}): {sendGridBody}";
        }
    }
}
