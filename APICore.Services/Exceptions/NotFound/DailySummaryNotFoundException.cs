namespace APICore.Services.Exceptions
{
    public class DailySummaryNotFoundException : BaseNotFoundException
    {
        public DailySummaryNotFoundException() : base()
        {
            CustomCode = 404090;
            CustomMessage = "No se encontró el cuadre diario para la fecha indicada.";
        }
    }
}
