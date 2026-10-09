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
    public class CategoryService : ICategoryService
    {
        private readonly Onion.DataAccess.Repositories.Concrete.IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<CategoryService> _logger;
        private readonly Onion.Common.Services.ICurrentUserService _currentUserService;

        public CategoryService(Onion.DataAccess.Repositories.Concrete.IUnitOfWork uow, IMapper mapper, ILogger<CategoryService> logger, Onion.Common.Services.ICurrentUserService currentUserService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task AddAsync(CategoryDto entityDto)
        {
            var entity = _mapper.Map<Category>(entityDto);
            // Set CompanyId from tenant context when creating
            entity.CompanyId = _currentUserService?.CompanyId ?? 0;
            await _uow.Categories.AddAsync(entity);
            await _uow.SaveChangesAsync();
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var list = await _uow.Categories.ListAsync();
            return _mapper.Map<IEnumerable<CategoryDto>>(list);
        }

        public async Task<CategoryDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Categories.GetByIdAsync(id);
            if (entity == null) return null;
            return _mapper.Map<CategoryDto>(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var existing = await _uow.Categories.GetByIdAsync(id);
            if (existing == null)
                throw new Onion.Common.Exceptions.CustomException(new Onion.Common.Models.Error { Code = "NOT_FOUND", Message = "Category not found", Language = "EN" });

            _uow.Categories.Remove(existing);
            await _uow.SaveChangesAsync();
        }
    }
}
