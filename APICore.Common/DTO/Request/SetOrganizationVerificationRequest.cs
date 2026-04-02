namespace APICore.Common.DTO.Request
{
    public class SetOrganizationVerificationRequest
    {
        public int OrganizationId { get; set; }
        public bool IsVerified { get; set; }
    }
}
