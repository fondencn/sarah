using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;

namespace Sarah.Ttn
{
    public class TtnClient :
        IDisposable
    {


        private readonly string _ClientID;
        private IManagedMqttClient _ManagedMqttClient;


        /// <summary>
        /// Application identifier.
        /// </summary>
        public string AppID { get; private set; }

        /// <summary>
        /// Value indicating whether this <see cref="T:TTNet.Data.App"/> is connected.
        /// </summary>
        public bool IsConnected => _ManagedMqttClient.IsConnected;

        public TtnClient(string appId)
        {
            AppID = appId;
            _ClientID = Guid.NewGuid().ToString();

            _ManagedMqttClient = new MqttFactory().CreateManagedMqttClient();
        }


        /// <summary>
        /// Start connection to The Things Network server.
        /// </summary>
        /// <param name="server">Server domain name.</param>
        /// <param name="port">Connection port.</param>
        /// <param name="withTls">Use TLS.</param>
        /// <param name="username">Username.</param>
        /// <param name="apiKey">API access key.</param>
        /// <param name="autoReconnectDelay">Time to wait after a disconnection to reconnect.</param>
        public async Task Start(string server, int port, bool withTls, string username, string apiKey, TimeSpan autoReconnectDelay)
        {
            /* Client aufbauen */
            await _ManagedMqttClient.StartAsync(new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(autoReconnectDelay)
                .WithClientOptions(GetMqttClientOptions(server, port, withTls, username, apiKey))
                .Build());

            _ManagedMqttClient.ApplicationMessageProcessedAsync += this.HandleApplicationMessageProcessedAsync;
            _ManagedMqttClient.ApplicationMessageReceivedAsync += this.HandleApplicationMessageReceivedAsync;
            _ManagedMqttClient.ConnectedAsync += this.HandleConnectedAsync;
            _ManagedMqttClient.ConnectingFailedAsync += this.HandleConnectingFailedAsync;

            /* Events abbonieren */
            // TODO: bei mehreren Geräten die konkrete DeviceId von ttn verwenden
            //v3/{application id}@{tenant id}/devices/{device id}/up
            //await _ManagedMqttClient.SubscribeAsync($"v3/{AppID}@ttn/devices/+/up");
            await _ManagedMqttClient.SubscribeAsync("#");



            //_ManagedMqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(e => Connected(this, e));
            //_ManagedMqttClient.UseDisconnectedHandler(async e => await Task.Run(() => Disconnected(this, e)));
            //_ManagedMqttClient.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate((Action<MqttApplicationMessageReceivedEventArgs>)OnApplicationMessageReceived);

            //_ManagedMqttClient.ApplicationMessageProcessedHandler = this;
            //_ManagedMqttClient.ApplicationMessageSkippedHandler = this;
            //_ManagedMqttClient.ConnectingFailedHandler = this;
            //_ManagedMqttClient.SynchronizingSubscriptionsFailedHandler = this;
        }

        /// <summary>
        /// Stop connection.
        /// </summary>
        public Task Stop() => _ManagedMqttClient.StopAsync();

        private MqttClientOptions GetMqttClientOptions(string server, int port, bool withTls, string username, string apiKey)
        {
            var tlsOptions = new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12 | (SslProtocols)12288, //tls1.3
                AllowUntrustedCertificates = true,
                IgnoreCertificateChainErrors = true,
                IgnoreCertificateRevocationErrors = true,
            };

            var o = new MqttClientOptionsBuilder()
                .WithClientId(_ClientID)
                .WithTcpServer(server, port)
                .WithCredentials(username, apiKey)
                .WithCleanSession();
            return withTls ? o.WithTlsOptions(tlsOptions).Build() : o.Build();
        }


        /// <summary>
        /// Dispose all resources used by this object
        /// </summary>
        public void Dispose() => _ManagedMqttClient.Dispose();


        public Task HandleApplicationMessageProcessedAsync(ApplicationMessageProcessedEventArgs eventArgs)
        {
            // _logger?.LogDebug("HandleApplicationMessageProcessedAsync");

            return Task.CompletedTask;
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            // _logger?.LogDebug("HandleApplicationMessageReceivedAsync");
            string topic = eventArgs.ApplicationMessage.Topic;
            string contentString = eventArgs.ApplicationMessage.ConvertPayloadToString();
            TtnMessage deserializedMessage = JsonConvert.DeserializeObject<TtnMessage>(contentString)!; // must not be null after deserialization, otherwise the message is not valid
            this.MessageReceived.Invoke(topic, deserializedMessage);
            return Task.CompletedTask;
        }

        public Task HandleConnectedAsync(MqttClientConnectedEventArgs eventArgs)
        {
            this.ConnectionStateChanged.Invoke(this, true);
            return Task.CompletedTask;
        }

        public Task HandleConnectingFailedAsync(ConnectingFailedEventArgs eventArgs)
        {
            this.ConnectionStateChanged.Invoke(this, false);
            return Task.CompletedTask;
        }

        public event EventHandler<bool> ConnectionStateChanged = delegate { };

        public event TtnMessageReceivedHandler MessageReceived = delegate { };
        public delegate void TtnMessageReceivedHandler(string topic, TtnMessage msg);
    }
}
