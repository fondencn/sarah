using Sarah.API.Interfaces;
using Sarah.Messaging.RabbitMQ.Messages;

namespace Sarah.Rules.Services;

public sealed class MessageBasedGridStateProvider : IGridStateProvider
{
    private readonly object _stateLock = new object();
    private int? _currentGridState;
    private string _currentGridStateText = "unbekannt";
    private string _zipCode = string.Empty;
    private DateTime? _lastChangedAtUtc;

    public int? CurrentGridState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentGridState;
            }
        }
    }

    public string CurrentGridStateText
    {
        get
        {
            lock (_stateLock)
            {
                return _currentGridStateText;
            }
        }
    }

    public string CurrentZipCode
    {
        get
        {
            lock (_stateLock)
            {
                return _zipCode;
            }
        }
    }

    public DateTime? LastChangedAtUtc
    {
        get
        {
            lock (_stateLock)
            {
                return _lastChangedAtUtc;
            }
        }
    }

    public void Update(GridStateChangedMessage msg)
    {
        lock (_stateLock)
        {
            _currentGridState = msg.CurrentState;
            _currentGridStateText = msg.CurrentStateText;
            _zipCode = msg.ZipCode;
            _lastChangedAtUtc = msg.ChangedAtUtc;
        }
    }
}
