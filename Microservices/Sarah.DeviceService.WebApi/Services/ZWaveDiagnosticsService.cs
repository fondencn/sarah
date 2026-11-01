using Microsoft.EntityFrameworkCore;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.DeviceService.WebApi.Data;
using ZWave.Channel.Protocol;
using Sarah.DeviceService.WebApi.DTOs;
using ZWave;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.WebApi.Services
{
    public interface IZWaveDiagnosticsService
    {
        /// <summary>Null when no ZWave controller is attached.</summary>
        Task<IReadOnlyList<ZWaveNodeDiagnosticsDto>?> GetNodesAsync(CancellationToken ct);
        ZWaveModeStatusDto GetModeStatus();
        Task<ZWaveModeStatusDto> StartModeAsync(ZWaveInclusionMode mode, CancellationToken ct);
        Task<ZWaveModeStatusDto> StopModeAsync(CancellationToken ct);
        bool IsKnownNode(byte nodeId);
        Task<ZWaveDeviceParametersDto> GetParametersAsync(byte nodeId, CancellationToken ct);
        Task<ZWaveParameterDto> SetParameterAsync(byte nodeId, byte paramId, long value, CancellationToken ct);
        Task<IReadOnlyList<ZWaveAssociationGroupDto>> GetAssociationGroupsAsync(byte nodeId, CancellationToken ct);
        Task SetAssociationGroupAsync(byte nodeId, byte groupId, byte[] nodeIds, CancellationToken ct);
    }

    public class ZWaveControllerUnavailableException() : InvalidOperationException("No ZWave controller available");

    public class ZWaveDeviceUnreachableException(string message, Exception inner) : InvalidOperationException(message, inner);

    public class ZWaveParameterNotSupportedException(byte paramId) : InvalidOperationException($"Parameter {paramId} is not known for this device type");

    public class ZWaveNodeNotFoundException(byte nodeId) : InvalidOperationException($"ZWave node {nodeId} not found");

    public class ZWaveDiagnosticsService : IZWaveDiagnosticsService
    {
        internal static readonly TimeSpan ModeTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan NodeQueryTimeout = TimeSpan.FromSeconds(5);
        // Reading every parameter of a device takes one round trip each.
        private static readonly TimeSpan ParameterTimeout = TimeSpan.FromSeconds(60);

        private readonly Sarah.DeviceService.DeviceService _deviceService;
        private readonly INodeFactory _nodeFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ZWaveDiagnosticsService> _logger;
        private readonly TimeProvider _time;
        private readonly SemaphoreSlim _modeLock = new(1, 1);

        private ZWaveInclusionMode _mode = ZWaveInclusionMode.None;
        private DateTimeOffset? _expiresAt;
        private CancellationTokenSource? _modeTimeoutCts;
        private ZWaveNetworkEventDto? _lastEvent;
        private ZWaveController? _subscribedController;

        public ZWaveDiagnosticsService(Sarah.DeviceService.DeviceService deviceService, INodeFactory nodeFactory, IServiceScopeFactory scopeFactory, ILogger<ZWaveDiagnosticsService> logger, TimeProvider? time = null)
        {
            _deviceService = deviceService;
            _nodeFactory = nodeFactory;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _time = time ?? TimeProvider.System;
        }

        public bool IsKnownNode(byte nodeId) => _nodeFactory.IsKnownNodeId(nodeId);

        public async Task<IReadOnlyList<ZWaveNodeDiagnosticsDto>?> GetNodesAsync(CancellationToken ct)
        {
            var controller = _deviceService.ZWaveControllerInternal;
            if (controller == null)
            {
                return null;
            }

            var nodes = await controller.GetNodes(ct).WaitAsync(NodeQueryTimeout, ct);
            var result = new List<ZWaveNodeDiagnosticsDto>();
            // The serial channel handles one request at a time, so query sequentially.
            foreach (var node in nodes.OrderBy(n => n.NodeID))
            {
                result.Add(await DescribeNodeAsync(node, ct));
            }
            return result;
        }

        private async Task<ZWaveNodeDiagnosticsDto> DescribeNodeAsync(Node node, CancellationToken ct)
        {
            var dto = new ZWaveNodeDiagnosticsDto
            {
                NodeId = node.NodeID,
                IsKnown = _nodeFactory.IsKnownNodeId(node.NodeID),
                ElementType = _deviceService.GetNetworkItem(node.NodeID)?.GetType().Name,
                Status = ZWaveNodeStatus.Unreachable
            };

            try
            {
                var info = await node.GetProtocolInfo(ct).WaitAsync(NodeQueryTimeout, ct);
                dto.IsListening = info.IsListening;
                dto.GenericType = info.GenericType.ToString();
                dto.SpecificType = info.SpecificType.ToString();

                var failed = await node.IsNodeFailed(ct).WaitAsync(NodeQueryTimeout, ct);
                if (failed)
                {
                    dto.Status = ZWaveNodeStatus.Failed;
                }
                else
                {
                    dto.Status = info.IsListening ? ZWaveNodeStatus.Alive : ZWaveNodeStatus.Sleeping;
                }

                var neighbours = await node.GetNeighbours(ct).WaitAsync(NodeQueryTimeout, ct);
                dto.Neighbors = neighbours.Select(n => (int)n.NodeID).OrderBy(id => id).ToArray();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ZWave diagnostics for node {NodeId} failed", node.NodeID);
                dto.Error = ex.Message;
            }
            return dto;
        }

        public ZWaveModeStatusDto GetModeStatus()
        {
            return new ZWaveModeStatusDto
            {
                ControllerAvailable = _deviceService.ZWaveControllerInternal != null,
                Mode = _mode,
                ExpiresAt = _expiresAt,
                LastEvent = _lastEvent
            };
        }

        public async Task<ZWaveModeStatusDto> StartModeAsync(ZWaveInclusionMode mode, CancellationToken ct)
        {
            if (mode == ZWaveInclusionMode.None)
            {
                throw new ArgumentException("Mode must be Inclusion or Exclusion", nameof(mode));
            }
            var controller = _deviceService.ZWaveControllerInternal ?? throw new ZWaveControllerUnavailableException();

            await _modeLock.WaitAsync(ct);
            try
            {
                if (_mode != ZWaveInclusionMode.None)
                {
                    await StopCurrentModeAsync(controller, ct);
                }

                SubscribeToNetworkChanges(controller);
                bool started = mode == ZWaveInclusionMode.Inclusion
                    ? await controller.StartAddingNodesToNetwork(ct)
                    : await controller.StartRemoveNodeFromNetwork(ct);
                if (!started)
                {
                    throw new InvalidOperationException($"Controller refused to start {mode} mode");
                }

                _mode = mode;
                _expiresAt = _time.GetUtcNow() + ModeTimeout;
                _modeTimeoutCts = new CancellationTokenSource();
                _ = AutoStopAsync(_modeTimeoutCts.Token);
                _logger.LogInformation("ZWave {Mode} mode started", mode);
            }
            finally
            {
                _modeLock.Release();
            }
            return GetModeStatus();
        }

        public async Task<ZWaveModeStatusDto> StopModeAsync(CancellationToken ct)
        {
            var controller = _deviceService.ZWaveControllerInternal ?? throw new ZWaveControllerUnavailableException();
            await _modeLock.WaitAsync(ct);
            try
            {
                await StopCurrentModeAsync(controller, ct);
            }
            finally
            {
                _modeLock.Release();
            }
            return GetModeStatus();
        }

        private async Task StopCurrentModeAsync(ZWaveController controller, CancellationToken ct)
        {
            var previous = _mode;
            _modeTimeoutCts?.Cancel();
            _modeTimeoutCts = null;
            _mode = ZWaveInclusionMode.None;
            _expiresAt = null;

            // Always send the stop commands so the controller leaves a mode started elsewhere, too.
            if (previous == ZWaveInclusionMode.Exclusion)
            {
                await controller.StopRemoveNodeFromNetwork(ct);
            }
            else
            {
                await controller.StopAddingNodesToNetwork(ct);
            }
            _logger.LogInformation("ZWave {Mode} mode stopped", previous);
        }

        private async Task AutoStopAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(ModeTimeout, _time, token);
                var controller = _deviceService.ZWaveControllerInternal;
                if (controller == null)
                {
                    return;
                }
                await _modeLock.WaitAsync(token);
                try
                {
                    if (_mode != ZWaveInclusionMode.None)
                    {
                        await StopCurrentModeAsync(controller, CancellationToken.None);
                    }
                }
                finally
                {
                    _modeLock.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // superseded by a manual stop or a new mode
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Automatic stop of ZWave inclusion/exclusion mode failed");
            }
        }

        private void SubscribeToNetworkChanges(ZWaveController controller)
        {
            if (ReferenceEquals(_subscribedController, controller))
            {
                return;
            }
            controller.NodesNetworkChanged += (_, e) =>
            {
                _lastEvent = new ZWaveNetworkEventDto
                {
                    NodeId = e.NodeId,
                    IsNodeAdded = e.IsNodeAdded,
                    IsKnown = _nodeFactory.IsKnownNodeId(e.NodeId),
                    Timestamp = _time.GetUtcNow()
                };
                _logger.LogInformation("ZWave node {NodeId} {Action}", e.NodeId, e.IsNodeAdded ? "included" : "excluded");
            };
            _subscribedController = controller;
        }

        public async Task<IReadOnlyList<ZWaveAssociationGroupDto>> GetAssociationGroupsAsync(byte nodeId, CancellationToken ct)
        {
            var association = GetAssociation(nodeId);
            var groups = await association.GetGroups(ct).WaitAsync(NodeQueryTimeout, ct);
            var result = new List<ZWaveAssociationGroupDto>();
            // Association group ids are 1-based.
            for (int id = 1; id <= groups.GroupsSupported; id++)
            {
                var report = await association.Get((byte)id, ct).WaitAsync(NodeQueryTimeout, ct);
                result.Add(new ZWaveAssociationGroupDto
                {
                    GroupId = (byte)id,
                    MaxNodesSupported = report.MaxNodesSupported,
                    Nodes = report.Nodes.Select(n => (int)n).ToArray()
                });
            }
            return result;
        }

        public async Task SetAssociationGroupAsync(byte nodeId, byte groupId, byte[] nodeIds, CancellationToken ct)
        {
            var association = GetAssociation(nodeId);
            var current = await association.Get(groupId, ct).WaitAsync(NodeQueryTimeout, ct);
            var wanted = nodeIds.Distinct().ToArray();

            foreach (var existing in current.Nodes.Where(n => !wanted.Contains(n)))
            {
                await association.Remove(groupId, ct, existing);
            }
            foreach (var added in wanted.Where(n => !current.Nodes.Contains(n)))
            {
                await association.Add(groupId, ct, added);
            }
        }

        public async Task<ZWaveDeviceParametersDto> GetParametersAsync(byte nodeId, CancellationToken ct)
        {
            EnsureNode(nodeId);
            var deviceType = await GetDeviceTypeAsync(nodeId, ct);
            var provider = _deviceService.GetParameterProvider(deviceType);
            var dto = new ZWaveDeviceParametersDto { DeviceType = deviceType.ToString(), HasProvider = provider != null };
            if (provider == null)
            {
                return dto;
            }

            var parameters = await TranslateUnreachable(() => provider.GetParameters(nodeId, _deviceService).WaitAsync(ParameterTimeout, ct));
            dto.Parameters = parameters.Select(ToDto).OrderBy(p => p.Id).ToList();
            return dto;
        }

        public async Task<ZWaveParameterDto> SetParameterAsync(byte nodeId, byte paramId, long value, CancellationToken ct)
        {
            EnsureNode(nodeId);
            var deviceType = await GetDeviceTypeAsync(nodeId, ct);
            var provider = _deviceService.GetParameterProvider(deviceType) ?? throw new ZWaveParameterNotSupportedException(paramId);

            return await TranslateUnreachable(async () =>
            {
                // The provider names only the parameters it knows; refuse to write arbitrary ids.
                var current = await provider.GetParameter(nodeId, paramId, _deviceService).WaitAsync(NodeQueryTimeout, ct);
                if (current.Name == "Unknown")
                {
                    throw new ZWaveParameterNotSupportedException(paramId);
                }

                await provider.SetParameter(nodeId, new DeviceParameter { Id = paramId, Name = current.Name, Value = value, Size = current.Size }, _deviceService)
                    .WaitAsync(NodeQueryTimeout, ct);
                var updated = await provider.GetParameter(nodeId, paramId, _deviceService).WaitAsync(NodeQueryTimeout, ct);
                return ToDto(updated);
            });
        }

        private static ZWaveParameterDto ToDto(DeviceParameter p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Value = Convert.ToInt64(p.Value),
            Size = p.Size
        };

        private void EnsureNode(byte nodeId)
        {
            if (_deviceService.ZWaveControllerInternal == null)
            {
                throw new ZWaveControllerUnavailableException();
            }
            if (_deviceService.GetNodeInternal(nodeId) == null)
            {
                throw new ZWaveNodeNotFoundException(nodeId);
            }
        }

        private async Task<KnownDeviceTypes> GetDeviceTypeAsync(byte nodeId, CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return await db.Devices
                .Where(d => d.NodeID == nodeId && d.SpecificType != KnownDeviceTypes.Unknown)
                .Select(d => d.SpecificType)
                .FirstOrDefaultAsync(ct);
        }

        // The providers wrap radio failures in a generic InvalidOperationException; surface them as "unreachable".
        private static async Task<T> TranslateUnreachable<T>(Func<Task<T>> action)
        {
            try
            {
                return await action();
            }
            catch (InvalidOperationException ex) when (ex.InnerException is TransmissionException)
            {
                throw new ZWaveDeviceUnreachableException(ex.Message, ex);
            }
        }

        private Association GetAssociation(byte nodeId)
        {
            if (_deviceService.ZWaveControllerInternal == null)
            {
                throw new ZWaveControllerUnavailableException();
            }
            var node = _deviceService.GetNodeInternal(nodeId) ?? throw new ZWaveNodeNotFoundException(nodeId);
            return node.GetCommandClass<Association>();
        }
    }
}
