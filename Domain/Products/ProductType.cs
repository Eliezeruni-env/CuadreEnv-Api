using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Onion.Domain.Products
{
    public class ProductType : BaseEntity
    {
        [MaxLength(100)]
        public string Description { get; set; }
    }
}
