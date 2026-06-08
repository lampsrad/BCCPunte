using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BCC.Pages
{
    public partial class FileUploadAbs
    {
        private string status = "";

        private async Task HandleFileSelected(InputFileChangeEventArgs e)
        {
            var file = e.File;
            if (file == null)
            {
                status = "No file selected.";
                return;
            }
            await UploadToHosting(file);
        }
        private async Task<string> Login(HttpClient client)
        {
            string username = config["Auth:Username"];
            string password = config["Auth:Password"];
            var loginData = new
            {
                username = username,
                password = password
            };
            var loginContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
            var loginResponse = await client.PostAsync($"{gData.Api}Auth/login", loginContent);
            var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
            return loginResult.Token;
        }
        private async Task UploadToHosting(IBrowserFile file)
        {
            var handler = new HttpClientHandler();
            using HttpClient client = new HttpClient(handler);
            string token = await Login(client);
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 120 * 1024 * 1024)); // 120 MB
            content.Add(fileContent, "file", file.Name);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            try
            {
                HttpResponseMessage response = null;
                if (file.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    response = await client.PostAsync($"{gData.Api}FU/html", content);
                if (file.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    response = await client.PostAsync($"{gData.Api}FU/zip", content);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    status = $"✅ {file.Name} {result}";
                }
                else
                {
                    status = $"❌ Upload failed: {response.ReasonPhrase}";
                }
            }
            catch (Exception ex)
            {
                status = $"Error: {ex.Message}";
                status = status + ex.InnerException.Message;
            }
        }
        public class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
        }
    }
}
