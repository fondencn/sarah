using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface IDeseaseStatsProvider
    {
        string DeseaseName { get; }
        string GetLocalStatsText();
    }
}
