using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sarah.API.Interfaces;
using Sarah.API.Interfaces.Service;
using Sarah.API.Interfaces.Services;
using Sarah.Data.Models;

namespace Sarah.Persons
{
    public class PersonService : IPersonService
    {
        private readonly ILogger<PersonService> _logger;
        private readonly IDBService _database;
        private readonly IDeviceService _devices;
        private readonly IGeoFenceService _geoFenceService;

        public PersonService(ILogger<PersonService> logger, IDBService database, IDeviceService deviceService, IGeoFenceService geoFenceService)
        {
            _logger = logger;
            _database = database;
            _devices = deviceService;
            _geoFenceService = geoFenceService;
        }


        public async Task<IEnumerable<IPerson>> GetAllPersonsAsync()
        {
            await HomeNetwork.Instance.Initialize();

            var persons = await _database.Persons
                .ToListAsync();
            persons.ForEach(p => LoadLocationInfos(p));
            return persons;
        }

        public async Task<IPerson?> GetPersonByIdAsync(int id)
        {
            await HomeNetwork.Instance.Initialize();

            var person = await _database.Persons
                .FirstOrDefaultAsync(p => p.Id == id);
            if(person != null) 
            {
                LoadLocationInfos(person);
            }
            return person;
        }


        public async Task AddPersonAsync(IPerson person)
        {
            if (person == null)
            {
            throw new ArgumentNullException(nameof(person));
            }

            var personEntity = person as  PersonInfo;
            if(personEntity == null) 
            {
                throw new ArgumentException("Person is not of type PersonInfo", nameof(person));
            }

            await _database.Persons.AddAsync(personEntity);
            await _database.SaveChangesAsync();

            _logger.LogInformation($"Person {person.Name} added successfully.");
        }

        public async Task DeletePersonAsync(int id)
        {
            var person = await _database.Persons.FirstOrDefaultAsync(p => p.Id == id);
            if (person == null)
            {
            throw new KeyNotFoundException($"Person with ID {id} not found.");
            }

            _database.Persons.Remove(person);
            await _database.SaveChangesAsync();

            _logger.LogInformation($"Person with ID {id} deleted successfully.");
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

            _logger.LogInformation($"Person {person.Name} updated successfully.");
        }

        private void LoadLocationInfos(PersonInfo p)
        {
            // Aktuelle GPS Tracker Position laden
            if(p.GPSTrackerID > 0) 
            {
                var device = _devices.GPSTrackers.FirstOrDefault(item => item.NodeID ==  p.GPSTrackerID);
                if(device != null) 
                {
                    p.Position = device.Position;
                }
            }

            // Prüfen ob Mobiltelefon der Person zu Hause ist
            if(p.MobilePhoneHostname != null) 
            {
                var device = HomeNetwork.Instance.KnownHosts?.FirstOrDefault(item => item.Hostname == p.MobilePhoneHostname);
                if(device != null) 
                {
                    p.IsAtHome = device.IsConnected;
                }

                if(p.Position == null && device != null) 
                {
                    p.IsAtHome |= _geoFenceService.GetCurrent(p.Position) == _geoFenceService.GetZuhause();
                }
            }
        }
    }
}