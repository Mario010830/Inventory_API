using System.Collections.Generic;

namespace APICore.Common.DTO.Response
{
    public class ChatAskResponse
    {
        public string Answer { get; set; } = string.Empty;
        public IReadOnlyList<string> Sources { get; set; } = new List<string>();
        public int TokensUsed { get; set; }
    }
}
