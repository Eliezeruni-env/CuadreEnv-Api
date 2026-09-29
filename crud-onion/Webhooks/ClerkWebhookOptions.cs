namespace crud_onion.Webhooks;

public sealed class ClerkWebhookOptions
{
    public string SigningSecret { get; set; } = string.Empty;
    public int TimestampToleranceSeconds { get; set; } = 300;
}
