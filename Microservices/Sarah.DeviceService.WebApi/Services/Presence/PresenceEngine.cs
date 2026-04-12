using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.DeviceService.Presence
{
    public class PresenceEngine : IPersonInfoProvider
    {
        #region singleton pattern
        private static PresenceEngine? _Instance;
        public static PresenceEngine Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new PresenceEngine();
                }
                return _Instance;
            }
        }

        private PresenceEngine()
        {
        }
        #endregion

        public IPersonInfoProvider? PersonInfos { get; set; }

        public bool IsPresent(string person) => PersonInfos?.IsPresent(person) == true;

        public bool IsSomeonePresent() => PersonInfos?.IsSomeonePresent() == true;
    }

    public interface IPersonInfoProvider
    {
        bool IsPresent(string person);
        bool IsSomeonePresent();
    }
}
