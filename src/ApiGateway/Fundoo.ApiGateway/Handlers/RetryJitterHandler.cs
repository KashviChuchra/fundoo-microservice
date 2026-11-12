//using System.Net;

//namespace Fundoo.ApiGateway.Handlers;

//public sealed class RetryJitterHandler : DelegatingHandler
//{
//    private const int MaxRetries = 2;
//    private const double BaseDelayMilliseconds = 200;
//    private const double MaxDelayMilliseconds = 2000;
//    private readonly ILogger<RetryJitterHandler> _logger;

//    public RetryJitterHandler(ILogger<RetryJitterHandler> logger)
//    {
//        _logger = logger;
//    }

//    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
//    {
//        // Retry only GET requests.
//        // POST, PUT, PATCH and DELETE pass through without retries.
//        if (request.Method != HttpMethod.Get)
//        {
//            return await base.SendAsync(request, cancellationToken);
//        }

//        // Save the original request details.
//        var requestUri = request.RequestUri;
//        var requestMethod = request.Method;
//        var requestVersion = request.Version;
//        var requestVersionPolicy = request.VersionPolicy;

//        var requestHeaders = request.Headers
//            .ToDictionary(
//                header => header.Key,
//                header => header.Value.ToArray());

//        var requestOptions = request.Options.ToArray();

//        for (var attempt = 0; ; attempt++)
//        {
//            cancellationToken.ThrowIfCancellationRequested();

//            // Each attempt needs a new HttpRequestMessage.
//            using var retryRequest = new HttpRequestMessage(requestMethod,requestUri)
//            {
//                Version = requestVersion,
//                VersionPolicy = requestVersionPolicy
//            };

//            // Copy request headers.
//            foreach (var header in requestHeaders)
//            {
//                retryRequest.Headers.TryAddWithoutValidation(
//                    header.Key,
//                    header.Value);
//            }

//            // Copy request metadata.
//            foreach (var option in requestOptions)
//            {
//                retryRequest.Options.Set(new HttpRequestOptionsKey<object?>(option.Key),option.Value);
//            }

//            HttpResponseMessage response;

//            try
//            {
//                response = await base.SendAsync(retryRequest,cancellationToken);
//            }
//            catch (HttpRequestException ex) when (attempt < MaxRetries)
//            {
//                var retryNumber = attempt + 1;
//                var delay = CalculateDelay(retryNumber);

//                _logger.LogWarning(ex,
//                    "Downstream GET request failed. Retry {RetryNumber}/{MaxRetries} " +
//                    "after {DelayMilliseconds} ms.",
//                    retryNumber,MaxRetries,delay.TotalMilliseconds);

//                await Task.Delay(delay, cancellationToken);

//                continue;
//            }

//            // Return immediately if the response isn't retryable
//            // or the maximum number of retries has been reached.
//            if (!ShouldRetry(response.StatusCode) || attempt >= MaxRetries)
//            {
//                return response;
//            }

//            var currentRetryNumber = attempt + 1;

//            var retryDelay = CalculateDelay(
//                currentRetryNumber,
//                response);

//            _logger.LogWarning(
//                "Downstream GET returned HTTP {StatusCode}. " +
//                "Retry {RetryNumber}/{MaxRetries} after {DelayMilliseconds} ms.",
//                (int)response.StatusCode, currentRetryNumber, MaxRetries, retryDelay.TotalMilliseconds);

//            // Dispose of the failed response before retrying.
//            response.Dispose();

//            await Task.Delay(retryDelay, cancellationToken);
//        }
//    }

//    private static bool ShouldRetry(HttpStatusCode statusCode)
//    {
//        return statusCode == HttpStatusCode.RequestTimeout       // 408
//            || statusCode == HttpStatusCode.TooManyRequests      // 429
//            || statusCode == HttpStatusCode.BadGateway           // 502
//            || statusCode == HttpStatusCode.ServiceUnavailable   // 503
//            || statusCode == HttpStatusCode.GatewayTimeout;      // 504
//    }

//    private static TimeSpan CalculateDelay(int retryNumber, HttpResponseMessage? response = null)
//    {
//        // Respect Retry-After when the downstream service provides it.
//        var retryAfter = response?.Headers.RetryAfter;

//        if (retryAfter?.Delta is TimeSpan delta)
//        {
//            return TimeSpan.FromMilliseconds(
//                Math.Clamp(delta.TotalMilliseconds, 0, 5000));
//        }

//        if (retryAfter?.Date is DateTimeOffset retryDate)
//        {
//            var milliseconds = (
//                retryDate - DateTimeOffset.UtcNow
//            ).TotalMilliseconds;

//            return TimeSpan.FromMilliseconds(
//                Math.Clamp(milliseconds, 0, 5000));
//        }

//        // Exponential backoff: 200 ms, 400 ms, ...
//        var exponentialDelay = Math.Min(BaseDelayMilliseconds * Math.Pow(2, retryNumber - 1),MaxDelayMilliseconds);
         
//        // Jitter: randomize the delay between 50% and 150%.
//        var jitteredDelay = exponentialDelay * (0.5 + Random.Shared.NextDouble());

//        return TimeSpan.FromMilliseconds(jitteredDelay);
//    }
//}