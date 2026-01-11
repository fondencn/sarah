using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Bekannte Gerätetypen, auf die speziell z.B. durch das Bearbeiten
    /// von Herstellerspezifischen Parametern reagiert werden kann
    /// </summary>
    public enum KnownDeviceTypes : long
    {
        /// <summary>
        /// Default, keine speziellen Eigenschaften
        /// </summary>
        Unknown = 0,
        FibaroMotionSensor = 1,
        AeotecDoorSensor = 2,
        AeotecZStick = 3,
        FibaroTheButton = 4,
        PoppWallController = 5,
        PoppWallPlug = 6,
        FibaroHeatController = 7,
        AeotecLedBulb = 8,
        FibaroRGBWController2 = 9,
        AeotecThermostat = 10,
        AeotecSmartSwitch7 = 11,
        AeotecLedBulb6White = 12,
        FibaroDoorWindowSensor2 = 13,
        FibaroWallPlug = 14,
        EutronicAirQualitySensor = 15,
        FibaroWalliSwitch = 16,
        FibaroSmokeSensor = 17,
        FibaroKeyFob = 18,
    }
}
