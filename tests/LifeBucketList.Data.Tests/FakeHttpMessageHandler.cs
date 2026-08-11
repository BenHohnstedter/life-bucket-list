using System.Net;
using System.Net.Http.Headers;

namespace LifeBucketList.Data.Tests;

public sealed record CapturedRequest(string Url, string Method, string? Body, HttpRequestHeaders Headers);

/// <summary>Routes HTTP requests to canned responses by URL substring, so HTTP-calling code can be
/// tested without any real network access.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<(string UrlContains, Func<CancellationToken, Task<HttpResponseMessage>> Respond)> _routes = new();
    public List<string> RequestedUrls { get; } = new();
    public List<CapturedRequest> Requests { get; } = new();

    public FakeHttpMessageHandler RespondWhenUrlContains(string urlSubstring, HttpStatusCode statusCode, string content)
    {
        _routes.Add((urlSubstring, _ => Task.FromResult(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content),
        })));
        return this;
    }

    public FakeHttpMessageHandler RespondWhenUrlContains(string urlSubstring, byte[] bytes)
    {
        _routes.Add((urlSubstring, _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes),
        })));
        return this;
    }

    public FakeHttpMessageHandler ThrowWhenUrlContains(string urlSubstring)
    {
        _routes.Add((urlSubstring, _ => throw new HttpRequestException("simulated network failure")));
        return this;
    }

    /// <summary>Never responds; waits until the request's own CancellationToken fires, then throws
    /// TaskCanceledException — simulates a request that's still in flight when the caller cancels it.</summary>
    public FakeHttpMessageHandler HangUntilCancelledWhenUrlContains(string urlSubstring)
    {
        _routes.Add((urlSubstring, async cancellationToken =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("unreachable");
        }));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        RequestedUrls.Add(url);

        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add(new CapturedRequest(url, request.Method.Method, body, request.Headers));

        var route = _routes.FirstOrDefault(r => url.Contains(r.UrlContains, StringComparison.Ordinal));
        if (route.Respond is null)
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        return await route.Respond(cancellationToken);
    }
}
