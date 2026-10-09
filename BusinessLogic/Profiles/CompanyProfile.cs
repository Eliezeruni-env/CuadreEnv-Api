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

            // Existing CompanySettings mapping (keep record-style CompanySettingsDto)
            CreateMap<CompanySettings, CompanySettingsDto>().ReverseMap();
            // Editable DTO mapping
            CreateMap<CompanySettings, CompanySettingsEditableDto>()
                .ForMember(d => d.CompanyName, opt => opt.MapFrom(s => s.CommercialName ?? string.Empty))
                .ForMember(d => d.InvoiceFooterPhrase, opt => opt.MapFrom(s => string.Empty))
                .ForMember(d => d.Rnc, opt => opt.Ignore())
                .ReverseMap()
                .ForMember(d => d.CommercialName, opt => opt.MapFrom(s => s.CompanyName));

            CreateMap<CreditNote, CreditNoteDto>()
                .ForMember(d => d.Details, opt => opt.MapFrom(s => s.Details));
            CreateMap<CreditNoteDetail, CreditNoteDetailDto>().ReverseMap();

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
