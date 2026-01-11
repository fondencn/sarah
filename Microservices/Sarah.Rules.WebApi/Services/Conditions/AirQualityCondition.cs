using Sarah.API.BusinessObjects;

namespace Sarah.Rules.Conditions
{
    /// <summary>
    /// Condition, die zutrifft, wenn die 
    /// im Event gemeldete Luftqualität nicht OK ist. 
    /// </summary>
    public class AirQualityCondition : RuleCondition
    {
        public AirQualityCondition(byte nodeId) : base(nodeId)
        {

        }

        public override bool Evaluate(NetworkEvent evt)
        {
            AirQualityChangedEvent airEvent = evt as AirQualityChangedEvent;
            if(airEvent != null)
            {
                if(evt.SourceNodeId == this.TargetNodeId)
                {
                    /* Condition löst aus, sobald die Leven schlechter als OK ist */
                    return airEvent.Level > AirQualitityLevel.OK;
                } 
                else
                {
                    /* falsche NodeId */
                    return false;
                }
            } 
            else
            {
                /* falscher Event Typ */
                return false;
            }

            //bool evalResult = false; ;

            //IMultiSensor sensor = InteLukNetworkFactory.InteLukNetwork.Sensors.FirstOrDefault(item => item.NodeID == this.TargetNodeId);
            //if(sensor == null)
            //{
            //    /* dann bin ich nicht betroffen */
            //    return false;
            //}

            //float? humidity = sensor.RelativeHumidity?.Value;
            //float? voc = sensor.VolatileOrganicCompounds?.Value;
            //float? co2 = sensor.CO2?.Value;

            //AirQualitityLevel humidityLevel = AirQualitityLevel.OK;
            //AirQualitityLevel co2Level = AirQualitityLevel.OK;
            //AirQualitityLevel vocLevel = AirQualitityLevel.OK;

            //if (humidity.HasValue)
            //{
            //    humidityLevel = AirQualityDefinitions.GetHumidityLevel(humidity.Value).Item1;
            //}
            //if (voc.HasValue)
            //{
            //    vocLevel = AirQualityDefinitions.GetVocLevel(voc.Value).Item1;
            //}
            //if (co2.HasValue)
            //{
            //    co2Level = AirQualityDefinitions.GetCo2Level(co2.Value).Item1;
            //}

            //evalResult = humidityLevel > AirQualitityLevel.OK || vocLevel > AirQualitityLevel.OK || co2Level > AirQualitityLevel.OK;

            //return evalResult;
        }
    }
}
