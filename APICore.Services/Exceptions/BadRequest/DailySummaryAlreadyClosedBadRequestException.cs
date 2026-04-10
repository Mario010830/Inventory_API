namespace APICore.Services.Exceptions
{
    public class DailySummaryAlreadyClosedBadRequestException : BaseBadRequestException
    {
        public DailySummaryAlreadyClosedBadRequestException() : base()
        {
            CustomCode = 400500;
            CustomMessage = "Ya existe un cuadre diario cerrado para esta fecha y localización.";
        }
    }
}
