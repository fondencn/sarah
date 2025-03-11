namespace Sarah.API.Interfaces
{
    public interface IWallController : INetworkElement
    {
        byte LastSceneId { get; }
    }
}
