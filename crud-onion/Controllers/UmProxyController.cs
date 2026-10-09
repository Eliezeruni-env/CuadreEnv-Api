using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text;
using System;

namespace Onion.Controllers
{
    [Route("um-api")]
    [ApiController]
    [Authorize]
    public class UmProxyController : ControllerBase
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly string _saasBase;
        private readonly string? _internalApiKey;

        public UmProxyController(IHttpClientFactory httpFactory, IConfiguration cfg)
        {
            _httpFactory = httpFactory;
            _saasBase = cfg["Saas:BaseUrl"]?.TrimEnd('/') ?? throw new InvalidOperationException("Saas:BaseUrl not configured");
            _internalApiKey = cfg["Internal:ApiKey"];
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string path, HttpContent? content = null)
        {
            var req = new HttpRequestMessage(method, _saasBase + path);
            if (content != null) req.Content = content;
            if (!string.IsNullOrWhiteSpace(_internalApiKey)) req.Headers.Add("X-Internal-ApiKey", _internalApiKey);
            return req;
        }

        private async Task<IActionResult> ForwardAsync(HttpRequestMessage req)
        {
            var client = _httpFactory.CreateClient();
            using var resp = await client.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) return StatusCode((int)resp.StatusCode);
            try
            {
                var json = JsonDocument.Parse(body).RootElement;
                return StatusCode((int)resp.StatusCode, json);
            }
            catch
            {
                return StatusCode((int)resp.StatusCode, body);
            }
        }

        // GET /um-api/users -> proxies to /internal/users
        [HttpGet("users")]
        public async Task<IActionResult> ListUsers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, [FromQuery] string? search = null, [FromQuery] bool? canLogin = null, [FromQuery] int? companyId = null)
        {
            var q = $"/internal/users?pageNumber={pageNumber}&pageSize={pageSize}";
            if (!string.IsNullOrWhiteSpace(search)) q += "&search=" + Uri.EscapeDataString(search);
            if (canLogin.HasValue && canLogin.Value) q += "&canLogin=true";
            if (companyId.HasValue) q += "&companyId=" + companyId.Value;

            var req = CreateRequest(HttpMethod.Get, q);
            return await ForwardAsync(req);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(string id)
        {
            var req = CreateRequest(HttpMethod.Get, $"/internal/users/{Uri.EscapeDataString(id)}");
            return await ForwardAsync(req);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] JsonElement body)
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var req = CreateRequest(HttpMethod.Post, "/internal/users", content);
            return await ForwardAsync(req);
        }



        [HttpPost("users/{id}/status")]
        public async Task<IActionResult> SetStatus(string id, [FromBody] JsonElement body)
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var req = CreateRequest(HttpMethod.Post, $"/internal/users/{Uri.EscapeDataString(id)}/status", content);
            return await ForwardAsync(req);
        }

        [HttpPost("users/{id}/subscription/by-plan-code")]
        public async Task<IActionResult> AssignSubscriptionByCode(string id, [FromBody] JsonElement body)
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var req = CreateRequest(HttpMethod.Post, $"/internal/users/{Uri.EscapeDataString(id)}/subscription/by-plan-code", content);
            return await ForwardAsync(req);
        }

        [HttpPatch("subscriptions/{subscriptionId}/next-payment")]
        public async Task<IActionResult> UpdateNextPayment(int subscriptionId, [FromBody] JsonElement body)
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var req = CreateRequest(HttpMethod.Patch, $"/internal/subscriptions/{subscriptionId}/next-payment", content);
            return await ForwardAsync(req);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] JsonElement body)
        {
            var content = new StringContent(body.GetRawText(), Encoding.UTF8, "application/json");
            var req = CreateRequest(HttpMethod.Put, $"/internal/users/{Uri.EscapeDataString(id)}", content);
            return await ForwardAsync(req);
        }
    }
}
