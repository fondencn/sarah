using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using Sarah.Logging;
using ZWave;
using ZWave.Channel.Protocol;
using ZWave.CommandClasses;

namespace Sarah.DeviceService.Model
{
    public abstract class AbstractParameterProvider : IParameterProvider
    {
        protected abstract string GetParameterName(byte paramId);
        protected abstract IEnumerable<byte> GetKnownParameters();


        public async Task<IEnumerable<DeviceParameter>> GetParameters(byte nodeId)
        {
            List<DeviceParameter> parameters = new List<DeviceParameter>();
            try
            {
                Node n = InteLukNetwork.Instance.GetNodeInternal(nodeId);
                Configuration configCmd = n.GetCommandClass<Configuration>();

                foreach (byte pId in GetKnownParameters())
                {
                    ConfigurationReport report = await configCmd.Get(pId);
                    parameters.Add(new DeviceParameter()
                    {
                        Id = pId,
                        Name = GetParameterName(pId),
                        Value = report.Value,
                        Size = report.Size
                    }); ;
                }

            }
            catch (TransmissionException tEx)
            {
                Logger.Instance.LogDebug("Error reading parameters, device is sleeping or not reachable: " + tEx.Message);
                throw new InvalidOperationException("Gerätekonfiguration konnte nicht ausgelesen werden, ggf. Gerät per Tastendruck aufwecken!", tEx);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("Error reading parameters: " + ex.Message);
                throw;
            }

            return parameters;
        }

        public async Task SetParameter(byte nodeId, DeviceParameter p)
        {
            try
            {
                Node n = InteLukNetwork.Instance.GetNodeInternal(nodeId);
                Configuration configCmd = n.GetCommandClass<Configuration>();
                await configCmd.Set(p.Id, Convert.ToByte(p.Value));
            }
            catch (TransmissionException tEx)
            {
                Logger.Instance.LogException("Error WRITING parameters, device is sleeping or not reachable", tEx);
                throw new InvalidOperationException("Gerätekonfiguration konnte nicht gesendet werden, ggf. Gerät per Tastendruck aufwecken!", tEx);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogException("Error reading parameters ", ex);
                throw;
            }
        }

        public async Task<DeviceParameter> GetParameter(byte nodeId, byte paramId)
        {
            DeviceParameter parameter = null;
            try
            {
                Node n = InteLukNetwork.Instance.GetNodeInternal(nodeId);
                Configuration configCmd = n.GetCommandClass<Configuration>();

                ConfigurationReport report = await configCmd.Get(paramId);
                parameter = new DeviceParameter()
                {
                    Id = paramId,
                    Name = GetParameterName(paramId),
                    Value = report.Value,
                    Size = report.Size
                };

            }
            catch (TransmissionException tEx)
            {
                Logger.Instance.LogDebug("Error reading parameters, device is sleeping or not reachable: " + tEx.Message);
                throw new InvalidOperationException("Gerätekonfiguration konnte nicht ausgelesen werden, ggf. Gerät per Tastendruck aufwecken!", tEx);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogDebug("Error reading parameters: " + ex.Message);
                throw;
            }

            return parameter;
        }
    }
}
