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
            var frame = new DefaultStompFrame(StompCommand.CONNECT);

            Log.Logger = new LoggerConfiguration()
                         .MinimumLevel.Debug()
                         .Enrich.FromLogContext()
                         .WriteTo.File("logs\\app.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
                         .CreateLogger();
            ExampleHelper.SetConsoleLogger();
            InternalLoggerFactory.DefaultFactory.AddSerilog(Log.Logger);

            Uri uri = new Uri("ws://172.19.136.161:30239/stomp");
            int port = 30239;

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

                var stompHandler = new StompClientHandler(uri.AbsoluteUri, string.Empty, string.Empty, string.Empty, string.Empty);

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
                            //pipeline.AddLast("client tls", tlsHandler);
                            pipeline.AddLast("http codec", new HttpClientCodec());
                            pipeline.AddLast("http aggregator", new HttpObjectAggregator(64*1024));
                            pipeline.AddLast("logging", new LoggingHandler(LogLevel.INFO));
                            pipeline.AddLast("websocket client handler", webSocketHandler);
                            pipeline.AddLast("websocket stomp decoder", new StompWebSocketDecoder(new ASCIIEncoding()));
                            pipeline.AddLast("websocket logger", new WebSocketLogger());
                            pipeline.AddLast("stomp decoder", new StompSubFrameDecoder());
                            pipeline.AddLast("websocket encoder", new StompWebSocketEncoder());
                            pipeline.AddLast("stomp heartbeart handler", new HeartBeatClientHandler(stompHandler));
                            pipeline.AddLast("aggregator", new StompSubFrameAggregator(1048576));
                            pipeline.AddLast("handler", stompHandler);
                        }));

                IChannel ch = await bootstrap.ConnectAsync(IPAddress.Parse(uri.Host), port);
                
                await webSocketHandler.HandshakeCompletion;
                Console.WriteLine("WebSocket handshake completed.");
                stompHandler.ActiveStomp(ch);
                Console.WriteLine("send connect stomp frame.");

                while (ch.IsWritable)
                {
                    Thread.Sleep(200);
                }

                await ch.CloseAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Raise Exception: {ex.Message}");
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }
    }
}
