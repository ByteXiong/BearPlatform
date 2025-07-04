using System.Net.Sockets;
using BearPlatform.Common.Helper.Serilog;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using Serilog;

namespace BearPlatform.EventBus.EventBusRabbitMQ;

/// <summary>
/// RabbitMQ 持久连接
/// </summary>
public class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
    private static readonly ILogger Logger = SerilogManager.GetLogger(typeof(RabbitMqPersistentConnection));
    private readonly IConnectionFactory _connectionFactory;
    private readonly int _retryCount;
    IConnection _connection;
    bool _disposed;
    object _syncRoot = new object();

    public RabbitMqPersistentConnection(IConnectionFactory connectionFactory, int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _retryCount = retryCount;
    }

    public bool IsConnected
    {
        get { return _connection != null && _connection.IsOpen && !_disposed; }
    }

    /// <summary>
    /// 创建rabbitmq模型
    /// </summary>
    /// <returns></returns>
    public IModel CreateModel()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
        }

        return _connection.CreateModel();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;

        try
        {
            _connection.ConnectionShutdown -= OnConnectionShutdown;
            _connection.CallbackException -= OnCallbackException;
            _connection.ConnectionBlocked -= OnConnectionBlocked;
            _connection?.Dispose();
        }
        catch (IOException ex)
        {
            Logger.Fatal(ex.ToString());
        }
    }

    /// <summary>
    /// 尝试连接
    /// </summary>
    /// <returns></returns>
    public bool TryConnect()
    {
        Logger.Information("RabbitMQ Client is trying to connect");

        lock (_syncRoot)
        {
            var policy = Policy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (ex, time) =>
                    {
                        Logger.Warning(
                            $"RabbitMQ Client could not connect after {time.TotalSeconds:n1}s ({ex.Message})");
                    }
                );

            policy.Execute(() =>
            {
                _connection = _connectionFactory
                    .CreateConnection();
            });

            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                Logger.Information(
                    $"RabbitMQ Client acquired a persistent connection to '{_connection.Endpoint.HostName}' and is subscribed to failure events");

                return true;
            }

            Logger.Fatal("FATAL ERROR: RabbitMQ connections could not be created and opened");
            return false;
        }
    }

    /// <summary>
    /// 连接阻塞
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;

        Logger.Warning("A RabbitMQ connection is shutdown. Trying to re-connect...");
        TryConnect();
    }

    /// <summary>
    /// 回调异常
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void OnCallbackException(object sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;

        Logger.Warning("A RabbitMQ connection throw exception. Trying to re-connect...");
        TryConnect();
    }

    /// <summary>
    /// 连接关闭
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="reason"></param>
    void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;

        Logger.Warning("A RabbitMQ connection is on shutdown. Trying to re-connect...");
        TryConnect();
    }
}
