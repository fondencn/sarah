namespace Sarah.Server.Models.Dtos;

public class NetworkElementDto
{
    public byte ID { get; internal set; }
    public string? TypeName { get; internal set; }
    public string? Name { get; internal set; }
    public string? Info { get; internal set; }
}
