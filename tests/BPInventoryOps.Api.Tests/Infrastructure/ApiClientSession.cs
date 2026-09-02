using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using BPInventoryOps.Api.Dtos.Auth;

namespace BPInventoryOps.Api.Tests.Infrastructure;

public sealed class ApiClientSession(HttpClient client) : IDisposable
{
    private string? _antiforgeryToken;

    public async Task<HttpResponseMessage> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        await RefreshAntiforgeryTokenAsync(cancellationToken);

        HttpResponseMessage response = await SendAsync(
            HttpMethod.Post,
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password },
            cancellationToken: cancellationToken);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            await RefreshAntiforgeryTokenAsync(cancellationToken);
        }

        return response;
    }

    public async Task RefreshAntiforgeryTokenAsync(
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response = await client.GetAsync(
            "/api/auth/antiforgery-token",
            cancellationToken);
        response.EnsureSuccessStatusCode();

        JsonNode body = JsonNode.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken))!;
        _antiforgeryToken = body["requestToken"]!.GetValue<string>();
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        bool includeAntiforgery = true,
        CancellationToken cancellationToken = default)
    {
        using HttpRequestMessage request = new(method, path);

        if (includeAntiforgery
            && _antiforgeryToken is not null
            && method != HttpMethod.Get
            && method != HttpMethod.Head
            && method != HttpMethod.Options)
        {
            request.Headers.TryAddWithoutValidation(
                "X-CSRF-TOKEN",
                _antiforgeryToken);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return await client.SendAsync(request, cancellationToken);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
