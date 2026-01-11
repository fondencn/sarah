using Sarah.API.BusinessObjects;
using System.Collections.Generic;

namespace Sarah.API.Interfaces
{
    public interface ICanSelfTest
    {
        IEnumerable<SelfTestResult> RunSelfTest();
    }
}
