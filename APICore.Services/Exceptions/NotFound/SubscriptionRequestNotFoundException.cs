namespace APICore.Services.Exceptions
{
    public class SubscriptionRequestNotFoundException : BaseNotFoundException
    {
        public SubscriptionRequestNotFoundException()
        {
            CustomCode = 404403;
            CustomMessage = "Solicitud de suscripción no encontrada.";
        }
    }
}
