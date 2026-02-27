using Microsoft.EntityFrameworkCore;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Services;
using Sarah.Persons.WebApi.Data;
using Sarah.Persons.WebApi.Data.Entities;
namespace Sarah.Persons.WebApi.Services
{
    public class PersonService : IPersonService
    {
        private readonly ILogger<PersonService> _logger;
        private readonly ApplicationDbContext _database;
        private readonly IDeviceService _deviceServiceClient;
        private readonly IGeoFenceService _geoFenceService;
        private readonly IConfiguration _config;
        private readonly HomeNetworkService _homeNetworkService;

        public PersonService(ILogger<PersonService> logger, ApplicationDbContext database, IDeviceService deviceService, IGeoFenceService geoFenceService, IConfiguration config, HomeNetworkService homenet)
        {
            _logger = logger;
            _database = database;
            _deviceServiceClient = deviceService;
            _geoFenceService = geoFenceService;
            _config = config;
            _homeNetworkService = homenet;
        }


        public async Task<IEnumerable<IPerson>> GetAllPersonsAsync()
        {
            await _homeNetworkService.Initialize(this._config);

            var persons = await _database.Persons
                .ToListAsync();
            foreach (var person in persons)
            {
                await LoadLocationInfos(person);
            }
            return persons;
        }

        public async Task<IPerson?> GetPersonByIdAsync(long id)
        {
            await _homeNetworkService.Initialize(this._config);

            var person = await _database.Persons
                .FirstOrDefaultAsync(p => p.Id == id);
            if(person != null) 
            {
                await LoadLocationInfos(person);
            }
            return person;
        }


        public async Task AddPersonAsync(IPerson person)
        {
            if (person == null)
            {
            throw new ArgumentNullException(nameof(person));
            }

            var personEntity = person as PersonInfoEntity;
            if(personEntity == null) 
            {
                // Create from interface
                personEntity = new PersonInfoEntity
                {
                    Name = person.Name,
                    GPSTrackerID = person.GPSTrackerID,
                    MobilePhoneHostname = person.MobilePhoneHostname
                };
            }

            await _database.Persons.AddAsync(personEntity);
            await _database.SaveChangesAsync();

            _logger.LogInformation("Person {PersonName} added successfully.", person.Name);
        }

        public async Task DeletePersonAsync(long id)
        {
            var person = await _database.Persons.FirstOrDefaultAsync(p => p.Id == id);
            if (person == null)
            {
            throw new KeyNotFoundException($"Person with ID {id} not found.");
            }

            _database.Persons.Remove(person);
            await _database.SaveChangesAsync();

            _logger.LogInformation("Person with ID {PersonId} deleted successfully.", id);
        }

        public async Task UpdatePersonAsync(IPerson person)
        {
            if (person == null)
            {
                throw new ArgumentNullException(nameof(person));
            }

            var personEntity = await _database.Persons.FirstOrDefaultAsync(p => p.Id == person.Id);
            if (personEntity == null)
            {
                throw new KeyNotFoundException($"Person with ID {person.Id} not found.");
            }

            personEntity.Name = person.Name;
            personEntity.GPSTrackerID = person.GPSTrackerID;
            personEntity.MobilePhoneHostname = person.MobilePhoneHostname;
            // Update other properties as needed

            _database.Persons.Update(personEntity);
            await _database.SaveChangesAsync();

            _logger.LogInformation("Person {PersonName} updated successfully.", person.Name);
        }

        public async Task<string[]> GetKnownHomeNetworkDevices()
        {
            await _homeNetworkService.Initialize(this._config);
            return _homeNetworkService.KnownHosts?
                .Select(item => item.Hostname)
                .Where(hostname => !string.IsNullOrWhiteSpace(hostname))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(hostname => hostname, StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
        }

        private async Task LoadLocationInfos(PersonInfoEntity p)
        {

            // Check if person's mobile phone is at home
            if(p.MobilePhoneHostname != null) 
            {
                var device = _homeNetworkService.KnownHosts?.FirstOrDefault(item => item.Hostname == p.MobilePhoneHostname);
                if(device != null) 
                {
                    p.IsAtHome = device.IsConnected;
                }
            }

            // Check GPS tracker position via DeviceService
            if (p.GPSTrackerID != 0)
            {
                var trackerDevice = await _deviceServiceClient.GetGpsTrackerByNodeId(p.GPSTrackerID);
                if (trackerDevice?.Position != null && trackerDevice.Position.IsValid)
                {
                    var position = new Sarah.API.BusinessObjects.LocatorPosition(
                        new Sarah.API.Business.SensorData(trackerDevice.Position.Longitude, "°"),
                        new Sarah.API.Business.SensorData(trackerDevice.Position.Latitude, "°"));
                    
                    var geofence = _geoFenceService.GetCurrent(position);
                    p.IsAtHome |= geofence == _geoFenceService.GetZuhause();
                    p.CurrentGeoFence = geofence;
                }
            }
            
        }

        public async Task<bool> IsSomeonePresent()
        {
            var persons = await this.GetAllPersonsAsync();
            return persons.Any(p => p.IsAtHome);
        }
    }
}