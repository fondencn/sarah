namespace Sarah.API.Interfaces
{
    public interface IEmailNotifier
    {
        void Send(string receipient, string subject, string body);
    }
}
