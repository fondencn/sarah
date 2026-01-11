using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Sarah.API.BusinessObjects;
using Sarah.LocationServer.Model;
using System.Collections.Generic;
using System.Security;

namespace Sarah.LocationServer.Controllers
{
    [ApiController]
    [Route("api/[controller]/")]
    public class LocationServiceController : ControllerBase
    {
        private readonly ILogger<LocationServiceController> _logger;

        public LocationServiceController(ILogger<LocationServiceController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Trace")]
        public Task<JsonResult> GetLocationTrace(string personName, string apiKey)
            => RunValidated(apiKey, () => Task.FromResult(new JsonResult(LocationServiceEntries.Instance[personName])));


        [HttpGet("Location")]
        public Task<JsonResult> GetLocation(string personName, string apiKey)
            => RunValidated(apiKey, () => Task.FromResult(new JsonResult(LocationServiceEntries.Instance[personName]?.FirstOrDefault())));


#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8604 // Dereference of a possibly null reference.
        [HttpPost]
        public Task AddLocation([FromBody] ProtectedLocationServiceEntry item)
             => RunValidated(item?.ApiKey, () =>
             {
                 item.DateTime = DateTime.Now; // Wert von außen überschreiben - wir haben immer recht!
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8604 // Dereference of a possibly null reference.
                 LocationServiceEntries.Instance.Add(item);
                 _logger.LogInformation("AddLocation {DeviceName} {PersonName} Bat={Battery} Lon={Longitude} Lat={Latitude}", 
                     item.DeviceName, item.PersonName, item.Battery, item.Longtitude, item.Latitude);
                 return Task.CompletedTask;
             });

        private Task<T> RunValidated<T>(string apiKey, Func<Task<T>> action)
        {
            if (!ApiValidator.IsValid(apiKey))
            {
                throw new SecurityException();
            }
            return action();
        }

        private Task RunValidated(string apiKey, Func<Task> action)
        {
            if (!ApiValidator.IsValid(apiKey))
            {
                throw new SecurityException();
            }
            return action();
        }
    }
}