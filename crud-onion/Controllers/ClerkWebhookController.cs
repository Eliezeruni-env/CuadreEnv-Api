using System.Text.Json;
using crud_onion.Webhooks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace crud_onion.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/webhooks/clerk")]
public sealed class ClerkWebhookController(SvixWebhookVerifier verifier, ClerkWebhookService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(cancellationToken);
        if (!verifier.IsValid(Request, body)) return Unauthorized();

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var eventType = root.TryGetProperty("type", out var type) ? type.GetString() : null;
        if (string.IsNullOrWhiteSpace(eventType)) return BadRequest(new { message = "El evento no contiene type." });
        if (root.TryGetProperty("data", out var data))
            await service.ProcessAsync(eventType, data, cancellationToken);

        return Ok(new { received = true });
    }
}
