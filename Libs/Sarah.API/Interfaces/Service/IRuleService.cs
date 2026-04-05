using Sarah.API.BusinessObjects;
using System.Collections.Generic;

namespace Sarah.API.Interfaces.Services {
    public interface IRuleService 
    {
        void RegisterRuleStore(IRuleStore storage);
        IEnumerable<Rule> Rules { get; }
    }
}