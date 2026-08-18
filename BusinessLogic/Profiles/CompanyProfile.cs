using AutoMapper;
using Onion.BussinesLogic.Dtos;
using Onion.Domain;

namespace Onion.BusinessLogic.Profiles
{
    public class CompanyProfile : Profile
    {
        public CompanyProfile()
        {
            CreateMap<Company, CompanyDto>()
                .ForMember(d => d.Settings, opt => opt.MapFrom(s => s.Settings));

            CreateMap<CompanySettings, CompanySettingsDto>().ReverseMap();

            CreateMap<CreateCompanyDto, Company>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Settings, opt => opt.Ignore());
            // also support CreateCompanyRequest used by services/controllers
            CreateMap<Onion.BussinesLogic.Dtos.CreateCompanyRequest, Company>()
                .ForMember(d => d.Id, opt => opt.Ignore())
                .ForMember(d => d.Settings, opt => opt.Ignore());
        }
    }
}
