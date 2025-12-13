using System.Net.Http.Headers;

namespace Web_Project.Services
{
    public class CookieForwardingHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                var cookieHeader = httpContext.Request.Headers["Cookie"].ToString();
                if (!string.IsNullOrWhiteSpace(cookieHeader))
                {
                    request.Headers.Remove("Cookie");
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
