using Onion.BusinessLogic.Dtos;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.BusinessLogic.Services.Abstract
{
    public interface ICategoryService
    {
        //Category GetEntity();
        Task AddAsync(CategoryDto entityDto);
        Task<IEnumerable<CategoryDto>> GetAllAsync();
        Task<CategoryDto?> GetByIdAsync(int id);
        Task DeleteAsync(int id);

    }
}
