using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace Onion.Domain.Products
{
    public class ProductType : BaseEntity
    {
        [MaxLength(100)]
        public string Description { get; set; }
        public int CompanyId { get; set; }
    }
}
