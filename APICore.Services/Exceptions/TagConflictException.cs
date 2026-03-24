using System.Net;

namespace APICore.Services.Exceptions
{
    /// <summary>
    /// 409 Conflict: nombre o slug de etiqueta ya existe.
    /// </summary>
    public class TagConflictException : CustomBaseException
    {
        public TagConflictException(string message) : base()
        {
            HttpCode = (int)HttpStatusCode.Conflict;
            CustomMessage = message;
        }
    }
}
