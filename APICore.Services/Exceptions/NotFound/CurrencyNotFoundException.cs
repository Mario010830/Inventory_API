namespace APICore.Services.Exceptions
{
    public class CurrencyNotFoundException : BaseNotFoundException
    {
        public CurrencyNotFoundException()
        {
            CustomCode = 404042;
            CustomMessage = "Moneda no encontrada.";
        }
    }
}
