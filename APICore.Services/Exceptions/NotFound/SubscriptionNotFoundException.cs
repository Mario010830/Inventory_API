namespace APICore.Services.Exceptions
{
    public class SubscriptionNotFoundException : BaseNotFoundException
    {
        public SubscriptionNotFoundException()
        {
            CustomCode = 404402;
            CustomMessage = "Suscripción no encontrada.";
        }
    }
}
