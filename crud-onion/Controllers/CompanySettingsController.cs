using Microsoft.AspNetCore.Mvc;
using Onion.Common.Services;
using Onion.DataAccess.Repositories.Concrete;
using Onion.BussinesLogic.Dtos;
using Onion.Domain;

namespace Onion.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompanySettingsController : ControllerBase
    {
        private readonly IUnitOfWork _uow;
        private readonly AutoMapper.IMapper _mapper;
        private readonly Onion.Common.Authorization.IAuthorizationService _auth;

        public CompanySettingsController(IUnitOfWork uow, AutoMapper.IMapper mapper, Onion.Common.Authorization.IAuthorizationService auth)
        {
            _uow = uow;
            _mapper = mapper;
            _auth = auth;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!_auth.TryGetCompanyId(User, out var companyId)) return BadRequest("CompanyId claim missing");

            var settingsList = await _uow.CompanySettingsRepo.FindAsync(s => s.CompanyId == companyId);
            var settings = settingsList.FirstOrDefault();
            if (settings == null) return NotFound();

            var dto = _mapper.Map<CompanySettingsDto>(settings);
            return Ok(dto);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CompanySettingsDto settingsDto)
        {
            if (!_auth.TryGetCompanyId(User, out var companyId)) return BadRequest("CompanyId claim missing");
            if (settingsDto.CompanyId != companyId) return Forbid();

            var settingsList = await _uow.CompanySettingsRepo.FindAsync(s => s.CompanyId == companyId);
            var settings = settingsList.FirstOrDefault();
            if (settings == null) return NotFound();

            // Map DTO into existing entity using AutoMapper
            _mapper.Map(settingsDto, settings);
            _uow.CompanySettingsRepo.Update(settings);
            await _uow.SaveChangesAsync();
            return NoContent();
        }
    }
}
