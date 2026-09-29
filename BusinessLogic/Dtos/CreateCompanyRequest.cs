using System.ComponentModel.DataAnnotations;

namespace Onion.BussinesLogic.Dtos
{
    public class CreateCompanyRequest
    {
        [Required, MinLength(2)]
        public string Name { get; set; } = string.Empty;
        public string? Rnc { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
    }
}
