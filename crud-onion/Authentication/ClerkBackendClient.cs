using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace crud_onion.Authentication;

public interface IClerkBackendClient
{
    Task<ClerkUserResponse?> GetUserAsync(string clerkUserId, CancellationToken cancellationToken = default);
}

public sealed class ClerkBackendClient(
    HttpClient httpClient,
    IOptions<ClerkOptions> options) : IClerkBackendClient
{
    private readonly ClerkOptions _options = options.Value;

    public async Task<ClerkUserResponse?> GetUserAsync(
        string clerkUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clerkUserId))
            throw new ArgumentException("A Clerk user ID is required.", nameof(clerkUserId));

        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException(
                "Clerk:SecretKey must be configured to call the Clerk Backend API.");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"users/{Uri.EscapeDataString(clerkUserId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SecretKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClerkUserResponse>(cancellationToken);
    }
}

public sealed class ClerkUserResponse
{
    public string Id { get; init; } = string.Empty;
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PrimaryEmailAddressId { get; init; }
    public IReadOnlyCollection<ClerkEmailAddress> EmailAddresses { get; init; } = [];
}

public sealed class ClerkEmailAddress
{
    public string Id { get; init; } = string.Empty;
    public string? EmailAddress { get; init; }
}
