using Onion.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace Onion.Domain
{
    public class BaseEntity : IAuditableEntity
    {
        public BaseEntity()
        {
            this.CreationDate = DateTime.Now;
            this.Active = true;
            this.CreateBy = string.Empty;
            this.ModifiedBy = string.Empty;
        }

        [Key]
        public int Id { get; set; }
        public DateTime CreationDate { get; set; }
        public bool Active { get; set; } = true;
        public bool IsDeleted { get; set; }
        public DateTime? ModificationDate { get; set; }
        public string CreateBy { get; set; }
        public string ModifiedBy { get; set; }
    }
}
