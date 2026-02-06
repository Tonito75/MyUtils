using Common.Logger;
using System.Security.Cryptography;
using Infrastructure;
using Microsoft.Extensions.Options;
using Infrastructure.Options;

namespace Application
{
    public class FreeBoxClient : IFreeBoxClient
    {
        private readonly ILogService _logger;
        private readonly HttpClient _httpClient = new();
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);

        private string _sessionToken = "";
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly TimeSpan _tokenValidityDuration = TimeSpan.FromMinutes(30); // Durée de validité estimée

        private string _baseUrl;
        private string _appId;
        private string _appToken;

        public FreeBoxClient(ILogService logService, IOptions<FreeBoxClientOptions> options)
        {
            _appToken = options.Value.AppToken;
            _appId = options.Value.AppId;
            _baseUrl = options.Value.BaseFreeBoxUrl;

            _logger = logService;
        }

        private async Task<(bool, string)> ConnectAsync()
        {
            _logger.Log("Connexion au client freebox...");

            await _connectionSemaphore.WaitAsync();
            try
            {
                // Vérifier si le token est encore valide
                if (IsTokenValid())
                {
                    _logger.Log($"Le token {_sessionToken} est encore valide, on le réutilise");
                    return (true, string.Empty);
                }

                _logger.Log("Le token a expiré ou n'est pas existant, une nouvelle connexion sera établie");

                // Getting the challenge
                var (challengeSuccess, challenge) = await GetChallenge();
                if (!challengeSuccess)
                {
                    return (false, challenge);
                }

                // Getting the token from the challenge
                var (getSessionTokenResult, token) = await GetSessionToken(challenge);
                if (!getSessionTokenResult)
                {
                    return (false, token);
                }

                _sessionToken = token;
                _tokenExpiry = DateTime.Now.Add(_tokenValidityDuration);

                _httpClient.DefaultRequestHeaders.Remove("X-Fbx-App-Auth");
                _httpClient.DefaultRequestHeaders.Add("X-Fbx-App-Auth", _sessionToken);

                _logger.Log("Connexion au client freebox réussie");

                return (true, string.Empty);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        private bool IsTokenValid()
        {
            return !string.IsNullOrEmpty(_sessionToken) && DateTime.Now < _tokenExpiry;
        }

        public async Task<(bool, string, List<FreeBoxDevice>?)> GetConnectedDevicesAsync()
        {
            // Vérifier et renouveler la connexion si nécessaire
            var (connectSuccess, connectError) = await EnsureConnectedAsync();
            if (!connectSuccess)
            {
                return (false, connectError, null);
            }

            try
            {
                var freeBoxDevicesUrl = $"{_baseUrl}/api/v10/lan/browser/pub/";
                _logger.Log($"Fetching depuis {freeBoxDevicesUrl}");

                var response = await _httpClient.GetAsync(freeBoxDevicesUrl);

                // Si on reçoit un 403, essayer de se reconnecter une fois
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.Log("Le status est 403 ! On tente la reconnexion");
                    _tokenExpiry = DateTime.MinValue; // Forcer la reconnexion

                    var (reconnectSuccess, reconnectError) = await EnsureConnectedAsync();
                    if (!reconnectSuccess)
                    {
                        return (false, reconnectError, null);
                    }

                    // Réessayer la requête
                    response = await _httpClient.GetAsync(freeBoxDevicesUrl);
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var results = doc.RootElement.GetProperty("result");
                var devices = new List<FreeBoxDevice>();

                foreach (var entry in results.EnumerateArray())
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var deviceDeserialized = entry.Deserialize<FreeBoxDevice>(options);
                    if (deviceDeserialized != null)
                    {
                        devices.Add(deviceDeserialized);
                    }
                }

                return (true, string.Empty, devices);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        private async Task<(bool, string)> EnsureConnectedAsync()
        {
            if (!IsTokenValid())
            {
                return await ConnectAsync();
            }
            return (true, string.Empty);
        }

        private async Task<(bool, string)> GetSessionToken(string challenge)
        {
            try
            {
                string password = ComputeHmacSha1(_appToken, challenge);

                var payload = new
                {
                    app_id = _appId,
                    password
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var freeBoxSessionUrl = $"{_baseUrl}/api/v10/login/session/";

                _logger.Log($"Fetching depuis {freeBoxSessionUrl}");

                var loginSessionResp = await _httpClient.PostAsync(freeBoxSessionUrl, content);
                var loginSessionJson = await loginSessionResp.Content.ReadAsStringAsync();

                _logger.Log($"Session retournée : {loginSessionJson}");

                using var sessionDoc = JsonDocument.Parse(loginSessionJson);

                var token = string.Empty;

                try
                {
                    token = sessionDoc.RootElement.GetProperty("result").GetProperty("session_token").GetString();
                }
                catch (Exception ex)
                {
                    return (false, $"Impossible de récupérer le token dans la réponse de la freebox {loginSessionJson} : {ex.Message}");
                }

                if (string.IsNullOrEmpty(token))
                {
                    return (false, "Le token de la session a été vide");
                }

                return (true, token);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private async Task<(bool, string)> GetChallenge()
        {
            try
            {
                var freeBoxLoginUrl = $"{_baseUrl}/api/v10/login/";

                _logger.Log($"Fetching depuis {freeBoxLoginUrl}");

                var loginChallengeResp = await _httpClient.GetAsync(freeBoxLoginUrl);

                if (!loginChallengeResp.IsSuccessStatusCode)
                {
                    return (false, $"Le status était {loginChallengeResp.StatusCode} avec le message : {loginChallengeResp.ReasonPhrase}");
                }

                string challenge = string.Empty;

                try
                {
                    var loginJson = await loginChallengeResp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(loginJson);
                    challenge = doc.RootElement.GetProperty("result").GetProperty("challenge").GetString();
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }

                if (string.IsNullOrEmpty(challenge))
                {
                    return (false, "Le challenge est null");
                }

                _logger.Log($"Challenge retourné : {challenge}");

                return (true, challenge);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private static string ComputeHmacSha1(string key, string message)
        {
            using var hmac = new HMACSHA1(Encoding.ASCII.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.ASCII.GetBytes(message));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _connectionSemaphore?.Dispose();
        }
    }
}