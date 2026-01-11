namespace Sarah.API.BusinessObjects.SpeakerRequests
{
    public class GetPersonLocationRequest
    {
        public string PersonName { get; set; } = string.Empty;
    }

    public class GetPersonLocationResponse
    {
        public string Message { get; set; }= string.Empty;
    }
}
