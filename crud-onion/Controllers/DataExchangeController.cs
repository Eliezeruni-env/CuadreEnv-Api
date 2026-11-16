using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Onion.BussinesLogic.Dtos;
using Onion.BussinesLogic.Services.Abstract;
using Onion.Common.Exceptions;
using Onion.Common.Models;
using Onion.Common.Services;
using Onion.Domain.DataExchange;

namespace Onion.Controllers
{
    [ApiController]
    [Route("data-exchange")]
    [Route("v1/data-exchange")]
    [Authorize]
    public class DataExchangeController : ControllerBase
    {
        private readonly IDataExchangeService _dataExchangeService;
        private readonly ICurrentUserService _currentUser;

        public DataExchangeController(IDataExchangeService dataExchangeService, ICurrentUserService currentUser)
        {
            _dataExchangeService = dataExchangeService ?? throw new ArgumentNullException(nameof(dataExchangeService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        private int GetCurrentCompanyId()
        {
            if (_currentUser.CompanyId is not > 0)
                throw new CustomException(new Error { Code = "TENANT_REQUIRED", Message = "Se requiere el contexto de la empresa.", Language = "ES" });
            return _currentUser.CompanyId.Value;
        }

        [HttpGet("templates/{entity}")]
        public async Task<IActionResult> DownloadTemplate(string entity, [FromQuery] string format = "xlsx")
        {
            var companyId = GetCurrentCompanyId();
            var (content, contentType, fileName) = await _dataExchangeService.GenerateTemplateAsync(companyId, entity, format);
            return File(content, contentType, fileName);
        }

        [HttpGet("export/{entity}")]
        public async Task<IActionResult> ExportData(string entity, [FromQuery] string format = "xlsx")
        {
            var companyId = GetCurrentCompanyId();
            var (content, contentType, fileName) = await _dataExchangeService.ExportDataAsync(companyId, entity, format);
            return File(content, contentType, fileName);
        }

        [HttpPost("preview/{entity}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PreviewImport(string entity, [FromForm] IFormFile file, [FromForm] ImportMode mode = ImportMode.CreateAndUpdate)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Debe seleccionar un archivo para importar."));
            }

            var companyId = GetCurrentCompanyId();
            var userId = _currentUser.UserId?.ToString() ?? "system";

            using var stream = file.OpenReadStream();
            var preview = await _dataExchangeService.PreviewImportAsync(companyId, entity, stream, file.FileName, mode, userId);
            return Ok(ApiResponse<ImportPreviewResponseDto>.Ok(preview));
        }

        [HttpPost("confirm/{batchId}")]
        public async Task<IActionResult> ConfirmImport(Guid batchId)
        {
            var companyId = GetCurrentCompanyId();
            var userId = _currentUser.UserId?.ToString() ?? "system";

            var result = await _dataExchangeService.ConfirmImportAsync(companyId, batchId, userId);
            return Ok(ApiResponse<ImportConfirmResponseDto>.Ok(result));
        }

        [HttpGet("errors/{batchId}")]
        public async Task<IActionResult> DownloadErrors(Guid batchId)
        {
            var companyId = GetCurrentCompanyId();
            var (content, contentType, fileName) = await _dataExchangeService.ExportErrorsAsync(companyId, batchId);
            return File(content, contentType, fileName);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            var companyId = GetCurrentCompanyId();
            var history = await _dataExchangeService.GetHistoryAsync(companyId);
            return Ok(ApiResponse<System.Collections.Generic.List<DataExchangeLogDto>>.Ok(history));
        }
    }
}
