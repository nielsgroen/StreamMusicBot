﻿using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;
using StreamMusicBot.Services;
using Serilog;
using Serilog.Formatting.Json;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Victoria;
using StreamMusicBot.Extensions;

namespace StreamMusicBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Start();

            try
            {
                Log.Information("Starting bot..");

                var spotifyClient = host.Services.GetRequiredService<SpotifyService>();
                Extensions.Extensions.Initialize(spotifyClient);

                #region the bot. app starts here.
                var botClient = host.Services.GetRequiredService<StreamMusicBotClient>();
                await botClient.InitializeAsync();
                #endregion
            }
            catch (Exception e)
            {
                Log.Fatal($"Host terminated unexpectedly. \n {e}", e);
            }
            finally
            {
                Log.CloseAndFlush();
            }

        }

        static IHost Start()
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                //.AddUserSecrets<StreamMusicBotClient>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) => {
                    services
                    .AddSingleton<StreamMusicBotClient>()
                    .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                     {
                         AlwaysDownloadUsers = true,
                         MessageCacheSize = 25,
                         LogLevel = LogSeverity.Debug,
                         //GatewayIntents = GatewayIntents.DirectMessages
                    }))
                    .AddSingleton(new CommandService(new CommandServiceConfig
                    {
                        LogLevel = LogSeverity.Verbose,
                        CaseSensitiveCommands = false
                    }))
                    .AddSingleton<MusicService>()
                    .AddSingleton(Configuration)
                    .AddSingleton<FavoritesService>()
                    .AddLavaNode(x => 
                        { 
                            x.SelfDeaf = false;
                            x.Port = Convert.ToUInt16(Configuration["lavaport"]); 
                            x.Hostname = Configuration["lavahostname"]; 
                            x.Authorization = Configuration["lavapass"]; 
                        })
                    .AddSingleton<TrackFactory>()
                    .AddSingleton<SpotifyService>();
                })
                .UseSerilog()
                .Build();
        }
    }
}
