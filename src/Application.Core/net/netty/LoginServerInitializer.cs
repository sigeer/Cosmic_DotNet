using DotNetty.Transport.Channels.Sockets;
using net.server.coordinator.session;

namespace net.netty;

public class LoginServerInitializer : ServerChannelInitializer
{
    protected override void InitChannel(ISocketChannel socketChannel)
    {
        string remoteAddress = getRemoteAddress(socketChannel);
        Log.Logger.Debug("Client connected to login server from {ClientIP} ", remoteAddress);

        PacketProcessor packetProcessor = PacketProcessor.getLoginServerProcessor();
        long clientSessionId = sessionId.getAndIncrement();

        var client = Client.createLoginClient(clientSessionId, socketChannel, packetProcessor, LoginServer.WORLD_ID, LoginServer.CHANNEL_ID);

        if (!SessionCoordinator.getInstance().canStartLoginSession(client))
        {
            socketChannel.CloseAsync().Wait();
            return;
        }

        initPipeline(socketChannel, client);
    }
}
