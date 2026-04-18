namespace APICore.Services.Exceptions
{
    public class CurrencyDenominationNotFoundException : BaseNotFoundException
    {
        public CurrencyDenominationNotFoundException()
        {
            CustomCode = 404043;
            CustomMessage = "Denominación no encontrada.";
        }
    }
}
