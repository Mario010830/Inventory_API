namespace APICore.Common.DTO.Request
{
    /// <summary>
    /// Verificación de organización en plataforma; replica el mismo valor en todas sus localizaciones.
    /// </summary>
    public class SetOrganizationVerificationRequest
    {
        public int OrganizationId { get; set; }
        public bool IsVerified { get; set; }
    }
}
