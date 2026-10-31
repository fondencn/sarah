import { getDeviceStatus } from './device-status';

describe('getDeviceStatus', () => {
  it('preserves device list state behavior using the existing sub-DTOs', () => {
    const lamp = getDeviceStatus({ lamp: { brightness: 128 } } as any);
    const thermostat = getDeviceStatus({ thermostat: { temperatureSetpoint: 20.5 } } as any);
    const door = getDeviceStatus({ doorSensor: { state: 1 } } as any, {
      extendedProperties: [
        { key: 'LastOpenDurationMinutes', value: '12' },
        { key: 'LastStateChanged', value: '2026-10-30T10:00:00Z' }
      ]
    } as any);
    const airQuality = getDeviceStatus({ airQuality: { co2: 650 } } as any);

    expect(lamp.category).toBe('lamp');
    expect(lamp.isOn).toBeTrue();
    expect(thermostat.category).toBe('thermostat');
    expect(thermostat.temperatureSetpoint).toBe(20.5);
    expect(door.doorIsOpen).toBeTrue();
    expect(door.lastOpenDurationMinutes).toBe(12);
    expect(door.lastStateChanged).toBe('2026-10-30T10:00:00Z');
    expect(airQuality.co2).toBe(650);
    expect(getDeviceStatus({} as any).category).toBe('unknown');
  });

  it('uses the detail endpoint ExtendedProperties for the full room status', () => {
    const status = getDeviceStatus(
      { deviceType: 10, thermostat: {} } as any,
      {
        extendedProperties: [
          { key: 'Temperature', value: '19.25' },
          { key: 'TemperatureSetpoint', value: '21' },
          { key: 'Battery', value: '87' }
        ]
      } as any
    );

    expect(status.category).toBe('thermostat');
    expect(status.temperature).toBe(19.25);
    expect(status.temperatureSetpoint).toBe(21);
    expect(status.battery).toBe(87);
  });

  it('preserves legacy list temperature fallbacks for thermostats and air-quality devices', () => {
    const thermostatStatus = getDeviceStatus({
      thermostat: { temperature: 19.5, temperatureSetpoint: 21 }
    } as any);
    const airQualityStatus = getDeviceStatus({
      airQuality: { temperature: 22.25, cO2: 640 }
    } as any);
    const detailedStatus = getDeviceStatus(
      { thermostat: { temperature: 19.5 } } as any,
      { extendedProperties: [{ key: 'Temperature', value: '20.75' }] } as any
    );

    expect(thermostatStatus.temperature).toBe(19.5);
    expect(airQualityStatus.temperature).toBe(22.25);
    expect(detailedStatus.temperature).toBe(20.75);
  });

  it('reads lamp, power, door and sensor state properties', () => {
    const lamp = getDeviceStatus({ deviceType: 8 } as any, {
      extendedProperties: [
        { key: 'IsOn', value: 'True' },
        { key: 'Brightness', value: '200' },
        { key: 'Color', value: '#ffeeaa' }
      ]
    } as any);
    const plug = getDeviceStatus({ deviceType: 6 } as any, {
      extendedProperties: [{ key: 'IsOn', value: 'False' }, { key: 'Meter_W', value: '42.3' }]
    } as any);
    const sensor = getDeviceStatus({ deviceType: 15 } as any, {
      extendedProperties: [
        { key: 'Temperature', value: '22' },
        { key: 'RelativeHumidity', value: '49' },
        { key: 'CO2', value: '750' },
        { key: 'VOC', value: '100' }
      ]
    } as any);

    expect(lamp.category).toBe('lamp');
    expect(lamp.isOn).toBeTrue();
    expect(lamp.brightness).toBe(200);
    expect(lamp.color).toBe('#ffeeaa');
    expect(plug.category).toBe('wallplug');
    expect(plug.isOn).toBeFalse();
    expect(plug.powerConsumption).toBe(42.3);
    expect(sensor.temperature).toBe(22);
    expect(sensor.humidity).toBe(49);
    expect(sensor.co2).toBe(750);
    expect(sensor.voc).toBe(100);
  });
});
