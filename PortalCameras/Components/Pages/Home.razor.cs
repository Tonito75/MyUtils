using BlazorApp.Models;
using Common.Classes;
using Microsoft.AspNetCore.Components;

namespace BlazorPortalCamera.Components.Pages
{
    public partial class Home : ComponentBase
    {
        private List<CameraConfig> _cameras = new();
        private Dictionary<string, bool?> _pingResults = new();
        private Dictionary<string, bool> _loadingStates = new();

        protected override void OnInitialized()
        {
            _cameras = CamerasOptions.Value ?? new List<CameraConfig>();
            Logger.LogInformation("Chargement de {Count} cameras depuis la configuration", _cameras.Count);

            foreach (var camera in _cameras)
            {
                _pingResults[camera.Name] = null;
                _loadingStates[camera.Name] = false;

                Task.Run(() => PingCamera(camera));
            }
        }

        private async Task PingCamera(CameraConfig camera)
        {
            Logger.LogInformation("Demarrage du ping pour la camera {CameraName}", camera.Name);

            _loadingStates[camera.Name] = true;
            _pingResults[camera.Name] = null;
            await InvokeAsync(StateHasChanged);

            var success = await PingService.PingAsync(camera.Ip);

            _pingResults[camera.Name] = success;
            _loadingStates[camera.Name] = false;

            if (success)
            {
                Logger.LogInformation("Ping reussi pour la camera {CameraName}", camera.Name);
            }
            else
            {
                Logger.LogWarning("Ping echoue pour la camera {CameraName}", camera.Name);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task LogToDiscord(CameraConfig camera)
        {
            Logger.LogInformation("Connexion à la caméra {CameraName}", camera.Name);

            await DiscordWebHookService.SendAsync($"{Emojis.Warn} Quelqu'un s'est connecté à la caméra {camera.Name}.");
        }
    }
}
