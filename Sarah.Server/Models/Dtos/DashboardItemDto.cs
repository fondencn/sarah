
namespace Sarah.Server.Models.Dtos
{
    public class DashboardItemDto
    {
        public static DashboardItemDto Default {get; } = new DashboardItemDto() {ItemId = -1, ItemType = DashboardItemType.Device, Title = "Default", Description = "Mark items as favorite to see them here"};
        public long ItemId { get; set; }
        public DashboardItemType ItemType {get;set;}
        public string Title { get; set; } = String.Empty;
        public string Description { get; set; } = String.Empty;
    }

    public enum DashboardItemType
    {
        Device,
        Scene,
        Room, 
        Person, 
        Weather
    }
}