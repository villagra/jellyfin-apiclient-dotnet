namespace Jellyfin.ApiClient.WebSocket
{
    public enum WebSocketState
    {
        None,
        Connecting,
        Open,
        CloseSent,
        CloseReceived,
        Closed,
        Aborted
    }
}
