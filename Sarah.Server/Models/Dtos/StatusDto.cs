using System.ComponentModel.DataAnnotations;

namespace Sarah.Server.Models.Dtos;

public class StatusDto
{
    public required string Hostname { get;  set; }
    public int Port { get;  set; }
    public bool IsAuthenticated { get;  set; }
    public string? Username {get;set;}
    
}
