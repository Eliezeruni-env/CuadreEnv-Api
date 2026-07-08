using System;

namespace Onion.BussinesLogic.Dtos
{
    public class CreateCompanyRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }
}
