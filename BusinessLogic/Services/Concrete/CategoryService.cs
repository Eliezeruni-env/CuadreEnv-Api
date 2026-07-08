using AutoMapper;
using Microsoft.Extensions.Logging;
using Onion.BusinessLogic.Dtos;
using Onion.BusinessLogic.Services.Abstract;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Concrete;
using Onion.Common.Exceptions;
using Onion.DataAccess.Repositories.Abstract;
using Onion.DataAccess.Repositories.Concrete;
using Onion.Domain.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.BusinessLogic.Services.Concrete
{
    public class CategoryService(
        ICategoryRepository categoryRepository,
        IMapper mapper,
        ILogger<CategoryService> logger) : ICategoryService
    {

        private readonly ICategoryRepository categoryRepository = categoryRepository;
        private readonly IMapper mapper = mapper;
        private readonly ILogger<CategoryService> logger = logger;
        public async Task AddAsync(CategoryDto entityDto)
        {
            var entity = mapper.Map<Category>(entityDto);
            await categoryRepository.AddAsync(entity);
        }
    }
}
