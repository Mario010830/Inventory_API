namespace APICore.Services.Exceptions
{
    public class PlanLimitExceededBadRequestException : BaseBadRequestException
    {
        public PlanLimitExceededBadRequestException(string message)
        {
            CustomCode = 400404;
            CustomMessage = message;
        }
    }
}
