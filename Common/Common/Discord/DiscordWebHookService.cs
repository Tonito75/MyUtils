using Common.Classes;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common.Discord
{
    public class DiscordWebHookService : IDiscordWebHookService
    {

        private readonly string _url;

        public DiscordWebHookService(IOptions<DiscordWebHookServiceOptions> options) {

            if (string.IsNullOrEmpty(options.Value.WebHookUrl))
            {
                throw new ArgumentNullException(nameof(options.Value.WebHookUrl));
            }

            _url = options.Value.WebHookUrl;
        }

        public Task LogInfoAsync(string text)
        {
            throw new NotImplementedException();
        }

        public Task LogErrorAsync(string text)
        {
            throw new NotImplementedException();
        }

        public async Task SendErrorAsync(string text)
        {
            await SendAsync($"{Emojis.RedCross} {text}");
        }

        public async Task SendOkAsync(string text)
        {
            await SendAsync($"{Emojis.Ok} {text}");
        }

        public async Task SendStopAsync(string text)
        {
            await SendAsync($"{Emojis.Stop} {text}");
        }

        public async Task SendStartAsync(string text)
        {
            await SendAsync($"{Emojis.Rocket} {text}");
        }

        public async Task SendWarnAsync(string text)
        {
            await SendAsync($"{Emojis.Warn} {text}");
        }

        public async Task SendAsync(string text)
        {
            using var httpClient = new HttpClient();
            var payload = new
            {
                content = text
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_url, content);
            response.EnsureSuccessStatusCode();
        }

        public async Task<(bool, string)> SendAsync(string filePath, string message)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                using var form = new MultipartFormDataContent();

                // Add message content if provided
                if (!string.IsNullOrEmpty(message))
                {
                    form.Add(new StringContent(message), "content");
                }

                // Read file and add to form
                var fileBytes = File.ReadAllBytes(filePath);
                var fileContent = new ByteArrayContent(fileBytes);

                // Set content type (you might want to detect this based on file extension)
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                // Add file with filename
                var fileName = Path.GetFileName(filePath);
                form.Add(fileContent, "file", fileName);

                var response = await httpClient.PostAsync(_url, form);

                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    return (true, string.Empty);
                }
                else
                {
                    var responseText = await response.Content.ReadAsStringAsync();
                    return (false, $"Status code ({(int)response.StatusCode}): {responseText}");
                }
            }
            catch (Exception e)
            {
                return (false, $"{e.Message}");
            }
        }

        
    }
}
