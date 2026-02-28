namespace APICore.Common.DTO.Request
{
    public class CreateContactRequest
    {
        public string Name { get; set; } = null!;
        public string? Company { get; set; }
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public string? Origin { get; set; }
        public bool IsActive { get; set; } = true;
        public int? AssignedUserId { get; set; }
    }
}
