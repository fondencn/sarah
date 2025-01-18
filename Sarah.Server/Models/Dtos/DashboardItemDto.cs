namespace Sarah.Server.Models.Dtos
{
    public class DashboardItemDto
    {
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