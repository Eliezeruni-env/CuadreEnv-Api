using AutoMapper;
using Onion.BussinesLogic.Dtos;
using Onion.Domain.Products;

namespace Onion.BusinessLogic.Mapping;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>();

        CreateMap<ProductDto, Product>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}