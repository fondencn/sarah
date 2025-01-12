using Sarah.API.BusinessObjects;

namespace Sarah.LocationServer.Model
{
    public static class ApiValidator
    {
        public static bool IsValid(string apiKey) => String.Equals(apiKey, LocationServiceApiKey.ApiKey, StringComparison.Ordinal);
    }
}
