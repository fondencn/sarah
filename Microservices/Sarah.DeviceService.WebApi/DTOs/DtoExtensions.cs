using Sarah.API.Business;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;

namespace Sarah.DeviceService.WebApi.DTOs
{
    public static class DtoExtensions
    {
        public static NetworkElementDto ToDto(this INetworkElement element)
        {
            return new NetworkElementDto
            {
                NodeID = element.NodeID,
                StateInfo = element.StateInfo,
                ClassDescription = element.ClassDescription,
                IsActive = element.IsActive
            };
        }

        public static NodeDto ToDto(this INode node)
        {
            return new NodeDto
            {
                DeviceTypeName = node.GetDeviceTypeName().GetAwaiter().GetResult()
            };
        }

        public static SensorDataDto? ToDto(this SensorData? data)
        {
            if (data == null)
            {
                return null;
            }

            return new SensorDataDto
            {
                Value = data.Value,
                Unit = data.Unit,
                Timestamp = data.Timestamp
            };
        }

        public static LampDto ToDto(this ILamp lamp)
        {
            return new LampDto
            {
                NodeID = lamp.NodeID,
                StateInfo = lamp.StateInfo,
                ClassDescription = lamp.ClassDescription,
                IsActive = lamp.IsActive,
                LastChange = lamp.LastChange,
                Brightness = lamp.Brightness,
                Color = lamp.Color,
                CurrentAnimation = lamp.CurrentAnimation?.GetType().Name
            };
        }

        public static WallPlugDto ToDto(this IWallPlug plug)
        {
            return new WallPlugDto
            {
                NodeID = plug.NodeID,
                StateInfo = plug.StateInfo,
                ClassDescription = plug.ClassDescription,
                IsActive = plug.IsActive,
                IsOn = plug.IsOn,
                LastChangeToPowerLow = plug.LastChangeToPowerLow,
                LastIncreasePower = plug.LastIncreasePower,
                LastDecreasePower = plug.LastDecreasePower
            };
        }

        public static MultiSensorDto ToDto(this IMultiSensor sensor)
        {
            return new MultiSensorDto
            {
                NodeID = sensor.NodeID,
                StateInfo = sensor.StateInfo,
                ClassDescription = sensor.ClassDescription,
                IsActive = sensor.IsActive,
                Luminance = sensor.Luminance.ToDto(),
                Presence = sensor.Presence.ToDto(),
                RelativeHumidity = sensor.RelativeHumidity.ToDto(),
                VolatileOrganicCompounds = sensor.VolatileOrganicCompounds.ToDto(),
                CO2 = sensor.CO2.ToDto()
            };
        }

        public static ThermoElementDto ToDto(this IThermoElement thermo)
        {
            return new ThermoElementDto
            {
                NodeID = thermo.NodeID,
                StateInfo = thermo.StateInfo,
                ClassDescription = thermo.ClassDescription,
                IsActive = thermo.IsActive,
                TemperatureSetpoint = thermo.TemperatureSetpoint.ToDto()
            };
        }

        public static DoorSensorDto ToDto(this IDoorSensor sensor)
        {
            return new DoorSensorDto
            {
                NodeID = sensor.NodeID,
                StateInfo = sensor.StateInfo,
                ClassDescription = sensor.ClassDescription,
                IsActive = sensor.IsActive,
                State = sensor.State,
                LastStateChanged = sensor.LastStateChanged,
                LastOpenDuration = sensor.LastOpenDuration
            };
        }

        public static SmokeSensorDto ToDto(this ISmokeSensor sensor)
        {
            return new SmokeSensorDto
            {
                NodeID = sensor.NodeID,
                StateInfo = sensor.StateInfo,
                ClassDescription = sensor.ClassDescription,
                IsActive = sensor.IsActive,
                IsSmokeDetected = sensor.IsSmokeDetected.ToDto(),
                IsOverheatingDetected = sensor.IsOverheatingDetected.ToDto(),
                Alarm = sensor.Alarm.ToDto()
            };
        }

        public static BatterySensorDto ToDto(this IBatterySensor sensor)
        {
            return new BatterySensorDto
            {
                NodeID = sensor.NodeID,
                Battery = sensor.Battery.ToDto()
            };
        }

        public static ControllerElementDto ToDto(this IControllerElement controller)
        {
            return new ControllerElementDto
            {
                NodeID = controller.NodeID,
                StateInfo = controller.StateInfo,
                ClassDescription = controller.ClassDescription,
                IsActive = controller.IsActive
            };
        }

        public static WallControllerDto ToDto(this IWallController controller)
        {
            return new WallControllerDto
            {
                NodeID = controller.NodeID,
                StateInfo = controller.StateInfo,
                ClassDescription = controller.ClassDescription,
                IsActive = controller.IsActive,
                LastSceneId = controller.LastSceneId
            };
        }

        public static UnknownElementDto ToDto(this IUnknownElement element)
        {
            return new UnknownElementDto
            {
                NodeID = element.NodeID,
                StateInfo = element.StateInfo,
                ClassDescription = element.ClassDescription,
                IsActive = element.IsActive
            };
        }

        public static LocatorPositionDto? ToDto(this LocatorPosition? position)
        {
            if (position == null)
            {
                return null;
            }

            return new LocatorPositionDto
            {
                Longtitude = position.Longtitude.ToDto(),
                Latitude = position.Latitude.ToDto(),
                MeasureTime = position.MeasureTime,
                IsValid = position.IsValid
            };
        }

        public static GpsTrackerDto ToDto(this IGPSTracker tracker)
        {
            return new GpsTrackerDto
            {
                NodeID = tracker.NodeID,
                StateInfo = tracker.StateInfo,
                ClassDescription = tracker.ClassDescription,
                IsActive = tracker.IsActive,
                Battery = tracker.Battery.ToDto(),
                Position = tracker.Position.ToDto(),
                PositionTrace = tracker.PositionTrace?
                    .Select(p => p.ToDto())
                    .Where(p => p != null)
                    .Select(p => p!)
                    .ToArray(),
                LastMessageReceived = tracker.LastMessageReceived,
                IsButtonPressed = tracker.IsButtonPressed.ToDto()
            };
        }

        public static AssociationGroupDto ToDto(this IAssociationGroup group)
        {
            return new AssociationGroupDto
            {
                GroupID = group.GroupID,
                Nodes = group.Nodes,
                MaxNodesSupported = group.MaxNodesSupported
            };
        }
    }
}
