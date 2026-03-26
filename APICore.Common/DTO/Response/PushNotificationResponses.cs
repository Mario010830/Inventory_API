namespace APICore.Common.DTO.Response
{
    public class PushOperationResponse
    {
        public bool Success { get; set; }
    }

    public class PushSendResultResponse
    {
        public int LocationId { get; set; }
        public int TotalSubscriptions { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Deactivated { get; set; }
        public string? Error { get; set; }
    }
}
