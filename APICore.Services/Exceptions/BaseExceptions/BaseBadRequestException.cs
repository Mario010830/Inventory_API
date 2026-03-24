using System.Net;

namespace APICore.Services.Exceptions
{
    public class BaseBadRequestException : CustomBaseException
    {
        public BaseBadRequestException() : base()
        {
            HttpCode = (int)HttpStatusCode.BadRequest;
        }

        public BaseBadRequestException(string message) : base(message)
        {
            HttpCode = (int)HttpStatusCode.BadRequest;
        }
    }
}