
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DiscordBot.Services.GetDevices
{
    public class GetDevicesServices(IOptions<GetDevicesServiceOptions> options) : IGetDevicesServices
    {
        private readonly HttpClient _httpClient = new();

        private readonly string _url = options.Value.Url;

        private readonly JsonSerializerOptions _serializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<(string? Error,List<LanDevice>? Devices)> GetDevices()
        {
            try
            {
                var response = await _httpClient.GetAsync(_url);

                var responseStr = await response.Content.ReadAsStringAsync();
                
                var devices = JsonSerializer.Deserialize<List<LanDevice>>(responseStr, _serializerOptions);

                return (Error: string.Empty, Devices: devices);
            }
            catch (Exception ex)
            {
                return (ex.Message, null);
            }
        }
    }
}
