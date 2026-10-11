#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sarah.API.BusinessObjects;
using Sarah.DeviceService.Model;
using Sarah.DeviceService.WebApi.Extensions;
using Sarah.Messaging.RabbitMQ;
using Xunit;

namespace Sarah.DeviceService.Tests;

public class ShellyTrvElementTests
{
    [Fact]
    public async Task SetTemperature_AcceptsSlowRpcResponseAndLogsRequestDuration()
    {
        int port = GetAvailablePort();
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        Task server = ServeRequestsAsync(listener, timeout.Token);
        var logger = new RecordingLogger();
        var thermostat = new ShellyTrvElement(
            253,
            $"127.0.0.1:{port}",
            200,
            new RecordingPublisher(),
            logger);

        try
        {
            await thermostat.SetTemperature(23);
            await server;

            string durationMessage = Assert.Single(logger.Messages, message =>
                message.Contains("SetTemperature HTTP completed in", StringComparison.Ordinal));
            string durationText = durationMessage
                .Split("SetTemperature HTTP completed in ", StringSplitOptions.None)[1]
                .Split("ms", StringSplitOptions.None)[0];
            Assert.True(long.TryParse(durationText, out long durationMs));
            Assert.InRange(durationMs, 10_000, 20_000);
        }
        finally
        {
            timeout.Cancel();
            listener.Close();
            try
            {
                await server;
            }
            catch (Exception ex) when (ex is OperationCanceledException
                or HttpListenerException
                or ObjectDisposedException)
            {
            }
        }
    }

    private static async Task ServeRequestsAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        HttpListenerContext setTemperatureRequest = await listener.GetContextAsync().WaitAsync(cancellationToken);
        Assert.Equal("POST", setTemperatureRequest.Request.HttpMethod);
        await Task.Delay(TimeSpan.FromSeconds(10.5), cancellationToken);
        await WriteResponseAsync(setTemperatureRequest, "{}");

        HttpListenerContext statusRequest = await listener.GetContextAsync().WaitAsync(cancellationToken);
        Assert.Equal("GET", statusRequest.Request.HttpMethod);
        await WriteResponseAsync(statusRequest, "{\"current_C\":20,\"target_C\":23,\"battery\":90,\"pos\":0}");
    }

    private static async Task WriteResponseAsync(HttpListenerContext context, string body)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body);
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
        context.Response.Close();
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class RecordingPublisher : NetworkElementPublisher
    {
        public RecordingPublisher()
            : base(new RabbitMQClient(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<RabbitMQClient>.Instance,
                new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>())
                    .Build()))
        {
        }

        public override Task ReportEvent<TValue>(NetworkElement element, string propertyName, [AllowNull] TValue newValue)
            => Task.CompletedTask;
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
