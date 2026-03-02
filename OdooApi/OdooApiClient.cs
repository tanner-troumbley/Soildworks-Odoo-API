using System.Text;
using System.Text.Json;

namespace OdooApi
{
    public class OdooApiClient : IDisposable
    {
        private readonly string _url;
        private readonly string _db;
        private readonly string _username;
        private readonly string _password;
        private readonly HttpClient _httpClient;
        private int _uid;

        public OdooApiClient(string url, string db, string username, string password)
        {
            _url = url.TrimEnd('/') ?? "https://example.odoo.com/";
            _db = db ?? "odoo-example-db";
            _username = username ?? "admin";
            _password = password ?? "admin";
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Authenticate with Odoo and store UID.
        /// </summary>
        public async Task<bool> AuthenticateAsync()
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    service = "common",
                    method = "authenticate",
                    args = new object[] { _db, _username, _password, new { } }
                },
                id = 1
            };

            var response = await PostAsync($"{_url}/jsonrpc", payload);
            if (response.RootElement.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Number)
            {
                _uid = result.GetInt32();
                return _uid > 0;
            }
            return false;
        }

        /// <summary>
        /// Search and read records from a model.
        /// </summary>
        public async Task<JsonElement> SearchReadAsync(string model, object[] domain, string[] fields, int limit = 0)
        {
            var kwargs = new { fields, limit };
            return await ExecuteKwAsync(model, "search_read", new object[] { domain }, kwargs);
        }

        /// <summary>
        /// Create a new record.
        /// </summary>
        public async Task<int> CreateAsync(string model, object values)
        {
            var result = await ExecuteKwAsync(model, "create", new object[] { values });
            return result.ValueKind == JsonValueKind.Number ? result.GetInt32() : -1;
        }

        /// <summary>
        /// Update existing records.
        /// </summary>
        public async Task<bool> UpdateAsync(string model, int[] ids, object values)
        {
            var result = await ExecuteKwAsync(model, "write", new object[] { ids, values });
            return result.ValueKind == JsonValueKind.True;
        }

        /// <summary>
        /// Delete records.
        /// </summary>
        public async Task<bool> DeleteAsync(string model, int[] ids)
        {
            var result = await ExecuteKwAsync(model, "unlink", new object[] { ids });
            return result.ValueKind == JsonValueKind.True;
        }

        /// <summary>
        /// Internal method to call Odoo execute_kw.
        /// </summary>
        private async Task<JsonElement> ExecuteKwAsync(string model, string method, object[] args, object kwargs = null)
        {
            var payload = new
            {
                jsonrpc = "2.0",
                method = "call",
                @params = new
                {
                    service = "object",
                    method = "execute_kw",
                    args = new object[]
                    {
                        _db,
                        _uid,
                        _password,
                        model,
                        method,
                        args,
                        kwargs ?? new { }
                    }
                },
                id = 2
            };

            var response = await PostAsync($"{_url}/jsonrpc", payload);
            if (response.RootElement.TryGetProperty("result", out var result))
                return result;

            throw new Exception("Odoo API call failed: " + response);
        }

        /// <summary>
        /// Helper to send JSON POST requests.
        /// </summary>
        private async Task<JsonDocument> PostAsync(string endpoint, object payload)
        {
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var httpResponse = await _httpClient.PostAsync(endpoint, content);
            httpResponse.EnsureSuccessStatusCode();
            var json = await httpResponse.Content.ReadAsStringAsync();
            return JsonDocument.Parse(json);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
