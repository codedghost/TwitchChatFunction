using System;
using CodedChatbot.ServiceBusContract;
using CodedChatbot.TwitchFactories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchLib.Client;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;

namespace TwitchChatFunction
{
    public class TwitchChatFunction
    {
        private readonly ILogger _logger;
        private ITwitchClient _client;

        private string _joinedChannel;

        public TwitchChatFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<TwitchChatFunction>();

            Reconnect();
        }

        [Function("TwitchChatFunction")]
        public async Task Run([ServiceBusTrigger("twitchchatqueue", Connection = "AzureWebJobsServiceBus")] string myQueueItem)
        {
            _logger.LogInformation($"C# ServiceBus queue trigger function processed message: {myQueueItem}");
            var message = JsonConvert.DeserializeObject<TwitchChat>(myQueueItem);

            if (message == null)
            {
                _logger.LogError($"Couldn't Deserialize message: {myQueueItem}");
                throw new Exception($"Couldn't Deserialize message: {myQueueItem}");
            }

            if (this._client == null || !this._client.IsConnected || !this._client.IsInitialized)
                this.Reconnect();

            await Task.Delay(1000);

            _client.SendMessage(_joinedChannel, message.Message);
        }

        private void Reconnect()
        {
            _logger.LogInformation("Reconnecting to Twitch");
            _client = new TwitchClient();
            var username = Environment.GetEnvironmentVariable("ChatbotUsername");
            var password = Environment.GetEnvironmentVariable("ChatbotPassword");
            _joinedChannel = Environment.GetEnvironmentVariable("StreamerChannel");
            _client.Initialize(new ConnectionCredentials(username, password), _joinedChannel, '!', '!', true);
            var result = _client.Connect();

            _logger.LogInformation(
                result ? "Successfully connected to Twitch Chat" : "Failed to connect to Twitch Chat");
        }
    }
}
