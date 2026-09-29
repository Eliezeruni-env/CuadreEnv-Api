using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Onion.BussinesLogic.Services.Concrete;

public interface ILicenseStartupValidator
{
    Task ValidateAsync(CancellationToken cancellationToken = default);
}

public sealed class LicenseStartupValidator : ILicenseStartupValidator
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<LicenseStartupValidator> _logger;

    public LicenseStartupValidator(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<LicenseStartupValidator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task ValidateAsync(CancellationToken cancellationToken = default)
    {
        var required = _configuration.GetValue<bool?>("Usm:ValidateOnStartup")
            ?? _environment.IsProduction();
        if (!required)
        {
            _logger.LogWarning("USM startup license validation is disabled for {Environment}.", _environment.EnvironmentName);
            return;
        }

        var baseUrl = _configuration["Usm:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Usm:BaseUrl is required when startup license validation is enabled.");

        var path = _configuration["Usm:LicenseVerifyPath"] ?? "api/license/verify";
        var method = (_configuration["Usm:LicenseVerifyMethod"] ?? "POST").ToUpperInvariant();
        var apiKey = _configuration["Usm:ApiKey"];
        var client = _httpClientFactory.CreateClient("Usm");
        var retryCount = Math.Clamp(_configuration.GetValue("Usm:StartupRetryCount", 2), 0, 5);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    method == "GET" ? HttpMethod.Get : HttpMethod.Post,
                    path.TrimStart('/'));
                if (!string.IsNullOrWhiteSpace(apiKey))
                    request.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                if (method != "GET")
                {
                    request.Content = JsonContent.Create(new
                    {
                        service = _configuration["Usm:ServiceName"] ?? "CuadreEnv",
                        instanceId = _configuration["Usm:InstanceId"] ?? Environment.MachineName,
                        environment = _environment.EnvironmentName
                    });
                }

                using var response = await client.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"USM license verification returned {(int)response.StatusCode} {response.StatusCode}.");

                using var document = JsonDocument.Parse(body);
                if (!TryReadBoolean(document.RootElement, "valid") &&
                    !TryReadBoolean(document.RootElement, "isValid") &&
                    !TryReadBoolean(document.RootElement, "licensed"))
                    throw new InvalidOperationException("USM license verification response did not contain a true valid/isValid/licensed flag.");

                _logger.LogInformation("USM license verified successfully for {Service}.", _configuration["Usm:ServiceName"] ?? "CuadreEnv");
                return;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= retryCount)
                    throw new InvalidOperationException("USM license verification timed out; startup is blocked by policy.", ex);
            }
            catch (HttpRequestException ex) when (attempt < retryCount)
            {
                _logger.LogWarning(ex, "USM license verification attempt {Attempt} failed; retrying.", attempt + 1);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException("USM license verification failed; startup is blocked by policy.", ex);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken);
        }
    }

    private static bool TryReadBoolean(JsonElement root, string propertyName) =>
        root.ValueKind == JsonValueKind.Object &&
        root.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.True;
}
