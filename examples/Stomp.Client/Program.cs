using System;

namespace Stomp.Client
{
    using System.Net;
    using System.Net.Security;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Codecs.Http;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using Examples.Common;
    using Serilog;
    using Serilog.Events;

    class Program
    {
        static void Main(string[] args) => RunClientAsync().Wait();

        static async Task RunClientAsync()
        {
            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .Enrich.FromLogContext()
                         .WriteTo.File("logs\\app.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                         .CreateLogger();
            ExampleHelper.SetConsoleLogger();
            InternalLoggerFactory.DefaultFactory.AddSerilog(Log.Logger);

            var auth = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE1NjU5MzczNDYsInRva2VuIjoidFZONHViaDdkdWxUNXBtM2pDc1IzdnNybTNFbmtjQkJYdi91bHhkazBuOUp2b2xWVERHZkxOaFpLWkRKSTFDbDZIclIvWURiQVZzWSt2Z2REUVVLWlZRRHZaTGRVS2ZXL21HeGg5aTZwbGxoRE5lblgwSndjVEtuczVIY0d2R2NFM0lHSDV3WUFRVk5OcVdJY1hKeTlHcm5SditWeENGWmozamE2SnFKRUJXV3VGQ0h5ZWtxYlVPZHIzT3BHM3M4R2xDWW9IT01UZWxlNUhFSnJDeGd6ZFhPbFp3eTFjb090bHVpaVZXL2E2MmVBRXNtRG52RVJLdDRJaU9vMnM5c1EyVTl1Yk5mR1djVUF0a240YytXUjRqMkZWT1JTajhXamM4dGRTakdPRHpqQXJFZE13YzNCODFLUGZOVHJpc2syS1ZSUFlFZFBXYy9jR0had2p5TTNHK3BwZE8xMU5YZkRoUGFneDZhSTd2QVBQVk1pREVqTUhDSUYvSVZhZVVNUG1oUXBaL1RwU2RJQzdqMk1hMnpSUT09In0.RJ3-SdAdjNxNsPexFL6qyLqhPle42ZnIcnrgfsJSA0Y";
            var id = "EopKAwd3Efy1RPEnHHsK";
            var passcode = "3VD83XjdKZj98LeoYc14";
            var topic = "/mchat/EopKAwd3Efy1RPEnHHsK";

            var uri = new Uri("wss://op-lime-pub.ncsoft.com/stomp");
            var port = 443;

            //var uri = new Uri("ws://127.0.0.1:8080/ws");
            //var port = 8080;

            var group = new MultithreadEventLoopGroup();

            try
            {
                
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Option(ChannelOption.TcpNodelay, true)
                    .Channel<TcpSocketChannel>();

                var webSocketHandler = new WebSocketClientHandler(
                    WebSocketClientHandshakerFactory.NewHandshaker(
                        uri,
                        WebSocketVersion.V13,
                        null,
                        false,
                        new DefaultHttpHeaders()));
                
                //var webSocketHandler = new WebSocketClientProtocolHandler(
                //    WebSocketClientHandshakerFactory.NewHandshaker(
                //        uri,
                //        WebSocketVersion.V13,
                //        null,
                //        false,
                //        new DefaultHttpHeaders()));

                var stompHandler = new StompClientHandler(uri.AbsoluteUri, id, passcode, topic, auth);

                bootstrap.Handler(
                    new ActionChannelInitializer<IChannel>(
                        channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            var tlsHandler = 
                                new TlsHandler(stream =>
                                {
                                    return new SslStream(stream, true, (sender, certificate, chain, errors) => true);
                                }, new ClientTlsSettings(uri.AbsoluteUri));

                            stompHandler.Channel = channel;

                            pipeline.AddLast("idle", new IdleStateHandler(0, 0, 0));
                            pipeline.AddLast("client tls", tlsHandler);
                            pipeline.AddLast("http codec", new HttpClientCodec());
                            pipeline.AddLast("http aggregator", new HttpObjectAggregator(64*1024));
                            pipeline.AddLast("logging", new LoggingHandler(LogLevel.INFO));
                            pipeline.AddLast("websocket client handler", webSocketHandler);
                            pipeline.AddLast("websocket stomp decoder", new StompWebSocketDecoder(new ASCIIEncoding()));
                            pipeline.AddLast("stomp decoder", new StompSubFrameDecoder());
                            pipeline.AddLast("websocket encoder", new StompWebSocketEncoder());
                            pipeline.AddLast("aggregator", new StompSubFrameAggregator(1048576));
                            pipeline.AddLast("handler", stompHandler);
                        }));

                IChannel ch = await bootstrap.ConnectAsync(uri.Host, port);
                await webSocketHandler.HandshakeCompletion;
                Console.WriteLine("WebSocket handshake completed.\n");
                stompHandler.ActiveStomp(ch);
                Console.WriteLine("send connect stomp frame.\n");

                while (ch.IsWritable)
                {
                    Thread.Sleep(200);
                }

                await ch.CloseAsync();
            }
            catch (Exception ex)
            {
                int i = 0;
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }
    }
}
