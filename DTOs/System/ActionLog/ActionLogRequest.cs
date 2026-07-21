using System;

namespace SystemBase.BE.DTOs.ActionLog
{
    public class ActionLogRequest
    {
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
