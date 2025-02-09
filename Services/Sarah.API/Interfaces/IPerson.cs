namespace Sarah.API.Interfaces
{
    public interface IPerson
    {
        long Id { get; set; }
        string Name { get; set; }
        byte GPSTrackerID { get; set; }
        string MobilePhoneHostname { get; set; }
    }
}