using Sarah.Monitoring.DTOs;

namespace Sarah.Monitoring.Clients
{
    public interface IRulesServiceClient
    {
        Task<RuleStatusDto?> GetStatusAsync();
    }
}
