namespace APICore.Services.Exceptions
{
    /// <summary>Suscripción en un estado que no permite la operación (rechazo vs cancelación).</summary>
    public class SubscriptionInvalidStateBadRequestException : BaseBadRequestException
    {
        public SubscriptionInvalidStateBadRequestException(int customCode, string message)
        {
            CustomCode = customCode;
            CustomMessage = message;
        }
    }
}
