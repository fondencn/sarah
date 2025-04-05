namespace Sarah.API.BusinessObjects
{
    public class DeviceParameter
    {
        public byte Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public object Value { get; set; }
        public byte Size { get; set; }
    }
}
