namespace APICore.Services.Exceptions
{
    public class SubscriptionRequestInvalidStateBadRequestException : BaseBadRequestException
    {
        public SubscriptionRequestInvalidStateBadRequestException(string message = "La solicitud no está en estado pendiente.")
        {
            CustomCode = 400402;
            CustomMessage = message;
        }
    }
}
