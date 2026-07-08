using AutoMapper;
using Onion.BusinessLogic.Dtos;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Products;
using System;
using System.Collections.Generic;
using System.Text;

namespace Onion.BusinessLogic.Profiles
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile()
        {
            CreateMap<Category, CategoryDto>();

            CreateMap<CategoryDto, Category>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());
        }
    }
}
