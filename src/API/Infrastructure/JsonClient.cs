using Serilog.Sinks.Http;

namespace API.Infrastructure
{
    public class JsonClient : IHttpClient
    {
        private const string JsonContentType = "application/json; charset=utf-8";

        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonHttpClient"/> class.
        /// </summary>
        public JsonClient()
            : this(new HttpClient())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonHttpClient"/> class with
        /// specified HTTP client.
        /// </summary>
        public JsonClient(HttpClient httpClient)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        ~JsonClient()
        {
            Dispose(false);
        }

        /// <inheritdoc />
        public virtual void Configure(IConfiguration configuration)
        {
        }

        /// <inheritdoc />
        public virtual async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
        {
            using var content = new StreamContent(contentStream);
            content.Headers.Add("Content-Type", JsonContentType);

            var response = await httpClient
                .PostAsync(requestUri, content)
                .ConfigureAwait(false);

            return response;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                httpClient.Dispose();
            }
        }
    }
}
