
using System.Text.Json.Nodes;

namespace Sarah.Server.Models.Dtos
{
    public class DashboardItemDto
    {
        /// <summary>
        /// Default dashboard item
        /// </summary>
        public static DashboardItemDto Default {get; } = new DashboardItemDto() {ItemId = -1, ItemType = DashboardItemTypeDto.Device, Title = "Default", Description = "Mark items as favorite to see them here", ExtendedProperties = new JsonObject()};
        
        
        public long ItemId { get; set; }
        public DashboardItemTypeDto ItemType {get;set;}
        public string Title { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
        public string Subtype { get; set; } = String.Empty;
        public required JsonObject ExtendedProperties {get;set;}
    }

    public enum DashboardItemTypeDto
    {
        Device,
        Scene,
        Room, 
        Person, 
        Weather
    }
}