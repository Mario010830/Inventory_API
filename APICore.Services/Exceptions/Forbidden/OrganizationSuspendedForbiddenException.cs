namespace APICore.Services.Exceptions
{
    public class OrganizationSuspendedForbiddenException : BaseForbiddenException
    {
        public OrganizationSuspendedForbiddenException()
        {
            CustomCode = 403401;
            CustomMessage = "Tu organización está pendiente de activación o suspendida. Contacta al administrador.";
        }
    }
}
