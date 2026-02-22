// using System;
// using System.Threading.Tasks;
// using System.Text.Json;
// using Microsoft.Extensions.Logging;
// using Sarah.API.BusinessObjects.DTOs;
// using Sarah.API.Interfaces.Services;
// using Sarah.API.Interfaces;

// namespace Sarah.ServiceClients
// {
//     public class RulesServiceClient : IRuleService
//     {
//         private readonly HttpClient _httpClient;
//         private readonly ILogger<RulesServiceClient> _logger;

//         public RulesServiceClient(HttpClient httpClient, ILogger<RulesServiceClient> logger)
//         {
//             _httpClient = httpClient;
//             _logger = logger;
//         }

//         public async Task<RuleStatusDto?> GetStatusAsync()
//         {
//             try
//             {
//                 var response = await _httpClient.GetAsync("/api/rules/status");
                
//                 if (response.IsSuccessStatusCode)
//                 {
//                     var json = await response.Content.ReadAsStringAsync();
//                     return JsonSerializer.Deserialize<RuleStatusDto>(json, new JsonSerializerOptions
//                     {
//                         PropertyNameCaseInsensitive = true
//                     });
//                 }
//                 else
//                 {
//                     _logger.LogWarning("Failed to get rules status. Status: {StatusCode}", response.StatusCode);
//                     return null;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error getting rules status");
//                 return null;
//             }
//         }

//         public void RegisterRuleStore(IRuleStore storage)
//         {
//             throw new NotImplementedException();
//         }
//     }
// }
