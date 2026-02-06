using Common.Classes;
using Discord;
using Discord.WebSocket;
using DiscordBot.DB;
using DiscordBot.Extensions;
using DiscordBot.Services.GetDevices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace DiscordBot
{
    public class Worker(ILogger<Worker> logger,
        IGetDevicesServices getDevicesServices,
        ApplicationDbContext context,
        DiscordSocketClient discordClient,
        IOptions<AppSettings> settings,
        BotService botService,
        IConfiguration config
        ) : BackgroundService
    {
        private readonly ILogger<Worker> _logger = logger;
        private readonly IGetDevicesServices _getDevicesServices = getDevicesServices;
        private readonly ApplicationDbContext _dbContext = context;
        private readonly DiscordSocketClient _discordClient = discordClient;
        private readonly IOptions<AppSettings> _settings = settings;
        private readonly BotService _botService = botService;

        private const int _delayMs = 4000;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Waiting for Discord client to be ready...");
            await _botService.Ready;
            _logger.LogInformation("Discord client is ready.");

            var channelId = (ulong)Convert.ToInt64(config["Discord:LanChannelId"]);
            var channel = _discordClient.GetChannel(channelId) as ISocketMessageChannel;

            var lanAlertRoleId = (ulong)Convert.ToInt64(config["Discord:LanAlertRoleId"]);

            if (channel == null)
            {
                _logger.LogError($"Can't find channel for id {channelId}");
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var (error,newDevices) = await _getDevicesServices.GetDevices();

                if (newDevices == null)
                {
                    _logger.LogError("No devices were returned.");
                    return;
                }
                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError($"Error while getting devices : {error}");
                    return;
                }

                try
                {
                    var existingDevices = await _dbContext.LanDevices.ToListAsync(stoppingToken);

                    var existingByMac = existingDevices.ToDictionary(e => e.MacAddress);

                    var newDeviceByMac = newDevices.ToDictionary(e => e.MacAddress);

                    var newDevicesToInsert = new List<LanDevice>();

                    foreach (var newDevice in newDevices)
                    {
                        if (existingByMac.TryGetValue(newDevice.MacAddress, out var oldDevice))
                        {
                            await SendUpdateAsync(channel, newDevice, oldDevice);

                            oldDevice.OriginalName = newDevice.OriginalName;
                            oldDevice.IsConnected = newDevice.IsConnected;
                            oldDevice.IpAddress = newDevice.IpAddress;
                            oldDevice.Vendor = newDevice.Vendor;
                            oldDevice.ConnectedSince = newDevice.ConnectedSince;
                            oldDevice.LastConnected = newDevice.LastConnected;

                        }
                        else
                        {
                            await SendAlertAsync(channel, newDevice, lanAlertRoleId);

                            newDevicesToInsert.Add(newDevice);
                        }
                    }

                    await _dbContext.AddRangeAsync(newDevicesToInsert, stoppingToken);
                    await _dbContext.SaveChangesAsync(stoppingToken);

                    await Task.Delay(_delayMs, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Unexpected error while getting devices : {ex.Message}");
                }
            }
            return;
        }

        private static async Task SendAlertAsync(ISocketMessageChannel channel, LanDevice device, ulong alertRoleId)
        {
            await channel.SendMessageAsync($"{Emojis.Warn} <@&{alertRoleId}> New device connected : {device}");
        }

        private static async Task SendUpdateAsync(ISocketMessageChannel channel, LanDevice newDevice, LanDevice oldDevice)
        {
            if (newDevice.IsConnected != oldDevice.IsConnected)
            {
                var message = newDevice.ToStringOnConnect();

                if (newDevice.IsConnected)
                {
                    await channel.SendMessageAsync($"{Emojis.Connected} Connected to lan : {message}");
                }
                else
                {
                    await channel.SendMessageAsync($"{Emojis.UnConnected} Disconnected from lan : {message}");
                }
            }
        }
    }
}
