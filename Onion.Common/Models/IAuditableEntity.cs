using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.Common.Models
{
    public interface IAuditableEntity
    {
        DateTime CreationDate { get; set; }
        DateTime? ModificationDate { get; set; }
        string CreateBy { get; set; }
        string ModifiedBy { get; set; }
    }
}
