using Sarah.API.BusinessObjects;
using Sarah.API.Interfaces;
using System;

namespace Sarah.Rules.Conditions
{
    internal class IsBeforeSunriseCondition : RuleCondition
    {
        private readonly IWeatherProvider _weather;
        public IsBeforeSunriseCondition(IWeatherProvider weather, byte targetNodeId) : base(targetNodeId)
        {
            this._weather = weather;
        }

        public override bool Evaluate(NetworkEvent evt)
        {
            return _weather.GetSunrise() > DateTime.Now;
        }
    }
}
