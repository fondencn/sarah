namespace Sarah.Server.Models.Dtos
{
    public class LocationDto 
    {
        public float Longitude { get; set; }
        public float Latitude { get; set; }
    }

    public class NamedLocationDto : LocationDto
    {
        public required string Name { get; set; } 
    }
}