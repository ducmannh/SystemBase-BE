using System;
using SystemBase.BE.DTOs.SystemFunction;

namespace SystemBase.BE.DTOs.SystemRole
{
    public class RoleFunctionDto : SystemFunctionDto
    {
        public bool IsGranted { get; set; }
    }
}
