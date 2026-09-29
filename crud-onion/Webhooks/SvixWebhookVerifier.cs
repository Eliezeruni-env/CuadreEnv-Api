using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace crud_onion.Webhooks;

public sealed class SvixWebhookVerifier(IOptions<ClerkWebhookOptions> options)
{
    private readonly ClerkWebhookOptions _options = options.Value;

    public bool IsValid(HttpRequest request, string body)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningSecret) ||
            !request.Headers.TryGetValue("svix-id", out var id) ||
            !request.Headers.TryGetValue("svix-timestamp", out var timestamp) ||
            !request.Headers.TryGetValue("svix-signature", out var signatures) ||
            !long.TryParse(timestamp.ToString(), out var unixTimestamp))
            return false;

        var eventTime = DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
        if (Math.Abs((DateTimeOffset.UtcNow - eventTime).TotalSeconds) > _options.TimestampToleranceSeconds)
            return false;

        var secret = _options.SigningSecret.StartsWith("whsec_", StringComparison.Ordinal)
            ? _options.SigningSecret[6..]
            : _options.SigningSecret;
        byte[] key;
        try { key = Convert.FromBase64String(secret); }
        catch (FormatException) { return false; }

        var signedContent = $"{id}.{timestamp}.{body}";
        var expected = Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedContent)));
        return signatures.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split(',', 2))
            .Any(parts => parts.Length == 2 && parts[0] == "v1" && FixedTimeEquals(parts[1], expected));
    }

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
