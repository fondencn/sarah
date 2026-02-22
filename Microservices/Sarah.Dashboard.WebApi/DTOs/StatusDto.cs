namespace Sarah.Dashboard.WebApi.DTOs;

/// <summary>
/// Data transfer object for the status endpoint
/// </summary>
public class StatusDto
{
    /// <summary>
    /// Hostname of the server
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Port of the server
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Whether the current request is authenticated
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Username of the authenticated user
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Status of the controller/service
    /// </summary>
    public string? ControllerStatus { get; set; }
}
