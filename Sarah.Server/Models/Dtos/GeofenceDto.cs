namespace Sarah.Server.Models.Dtos
{
    public class GeofenceDto
    {
        public int Id { get; set; } = 0;
        public required string Name { get; set; }
        public required List<PointDto> Points { get; set; } 
    }

    public class PointDto
    {
        public float Latitude { get; set; }
        public float Longitude { get; set; }
    }
}