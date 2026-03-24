namespace APICore.Common.DTO.Response
{

    public class InventoryMovementFormContextResponse
    {
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        public bool IsLocationLocked { get; set; }
    }
}
