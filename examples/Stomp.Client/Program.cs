using System;

namespace Stomp.Client
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Net;
    using System.Net.Security;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs;
    using DotNetty.Codecs.Compression;
    using DotNetty.Codecs.Http;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Http.WebSockets.Extensions;
    using DotNetty.Codecs.Http.WebSockets.Extensions.Compression;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Handlers.Logging;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Handlers.Tls;
    using DotNetty.Transport.Bootstrapping;
    using DotNetty.Transport.Channels;
    using DotNetty.Transport.Channels.Sockets;
    using DotNetty.Transport.Libuv;
    using Examples.Common;
    using Microsoft.Extensions.Logging;
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

            

            var auth = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpYXQiOjE1NjUwOTQ5MzAsInRva2VuIjoidFZONHViaDdkdWxUNXBtM2pDc1IzdnNybTNFbmtjQkJYdi91bHhkazBuOUp2b2xWVERHZkxOaFpLWkRKSTFDbDZIclIvWURiQVZzWSt2Z2REUVVLWlZRRHZaTGRVS2ZXL21HeGg5aTZwbGxoRE5lblgwSndjVEtuczVIY0d2R2NFM0lHSDV3WUFRVk5OcVdJY1hKeTlHcm5SditWeENGWmozamE2SnFKRUJXV3VGQ0h5ZWtxYlVPZHIzT3BHM3M4R2xDWW9IT01UZWxlNUhFSnJDeGd6ZFhPbFp3eTFjb090bHVpaVZXL2E2MmVBRXNtRG52RVJLdDRJaU9vMnM5c1EyVTl1Yk5mR1djVUF0a240YytXUjdKQUxCTllLazhZbVVFbi8reXBSeThYb1hORkVtbGJvN2x1UnJmcmNqM05wWXdoNFZJRFJPV2t0N3YzVjdyZTF3OXJWT2I5cXNDVURCK1ptSllsSSsva0FqdTNQS25BSkdpZ1NvNWd2MFRRIn0.I8PSOabUuw1UoY3d_19Mqv2uQnI0OddszZQJeacpsSQ";
            var id = "lYTW1hy2LHcL0DMucnuS";
            var passcode = "w28BuzjmZCJRgvp2YMm9";
            var topic = "/mchat/lYTW1hy2LHcL0DMucnuS";

            //var uri = new Uri("wss://op-lime-pub.ncsoft.com/stomp");
            //var port = 443;

            var uri = new Uri("ws://127.0.0.1:8080/ws");
            var port = 8080;

            var group = new MultithreadEventLoopGroup();

            try
            {
                
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Option(ChannelOption.TcpNodelay, true)
                    .Channel<TcpSocketChannel>();

                //var webSocketHandler = new WebSocketClientHandler(
                    //WebSocketClientHandshakerFactory.NewHandshaker(
                    //    uri,
                    //    WebSocketVersion.V13,
                    //    null,
                    //    true,
                    //    new DefaultHttpHeaders()));
                var stompHandler = new StompClientHandler(uri.AbsoluteUri, id, passcode, topic, auth);

                var webSocketHandler = new WebSocketClientProtocolHandler(
                    WebSocketClientHandshakerFactory.NewHandshaker(
                        uri,
                        WebSocketVersion.V13,
                        null,
                        false,
                        new DefaultHttpHeaders()));
                var stompHandler = new StompClientHandler(uri.AbsoluteUri, id, passcode, topic, auth);

                bootstrap.Handler(
                    new ActionChannelInitializer<IChannel>(
                        channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            var tlsHandler = 
                                new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(uri.AbsoluteUri));

                            stompHandler.Channel = channel;

                            pipeline.AddLast("idle", new IdleStateHandler(0, 0, 0));
                            //pipeline.AddLast("client tls", tlsHandler);
                            pipeline.AddLast("logging", new LoggingHandler());
                            pipeline.AddLast("http codec", new HttpClientCodec());
                            pipeline.AddLast("http aggregator", new HttpObjectAggregator(64*1024));
                            //pipeline.AddLast("websocket handler", WebSocketClientCompressionHandler.Instance);
                            pipeline.AddLast("websocket client handler", webSocketHandler);
                        }));

                IChannel ch = await bootstrap.ConnectAsync(uri.Host, port);
                await webSocketHandler.Handshaker.HandshakeAsync(ch);
                stompHandler.ActiveStomp(ch);

                Console.WriteLine("WebSocket handshake completed.\n");
                Console.WriteLine("\t[bye]:Quit \n\t [ping]:Send ping frame\n\t Enter any text and Enter: Send text frame");

                while (true)
                {
                    Thread.Sleep(100);
                    string msg = Console.ReadLine();
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
    public class StompWebSocketDecoder : MessageToMessageDecoder<WebSocketFrame>
    {
        public string Host { get; }

        public string Login { get; }

        public string Passcode { get; }

        public string Topic { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="StringEncoder" /> class with the specified character
        ///     set..
        /// </summary>
        /// <param name="encoding">Encoding.</param>
        public StompWebSocketDecoder(Encoding encoding)
        {
            this.encoding = encoding ?? throw new NullReferenceException("encoding");
        }

        /// <inheritdoc />
        public StompClientHandler(string host, string login, string passcode, string topic, string auth)
        {
            this.Host = host;
            this.Login = login;
            this.Passcode = passcode;
            this.Topic = topic;
            this.Authorization = auth;
        }
    }

    public class StompSubFramePublicEncoder : StompSubFrameEncoder
    {
        public void Encode(IChannelHandlerContext context, IStompSubFrame message, List<object> output)
        {
            base.Encode(context, message, output);
        }
    }


    public class StompWebSocketEncoder : MessageToMessageEncoder<IStompSubFrame>
    {
        StompSubFramePublicEncoder encoder = new StompSubFramePublicEncoder();

        public void ActiveStomp(IChannel channel)
        {
            var _channel = channel;

            this._state = ClientState.AUTHENTICATING;
            IStompFrame connFrame = new DefaultStompFrame(StompCommand.CONNECT);
            connFrame.Headers.Set(StompHeaderNames.AcceptVersion, new AsciiString("1.0,1.1,1.2"));
            //connFrame.Headers.Set(StompHeaderNames.Host, new AsciiString(Host));
            connFrame.Headers.Set(StompHeaderNames.Login, new AsciiString(Login));
            connFrame.Headers.Set(StompHeaderNames.PassCode, new AsciiString(Passcode));
            connFrame.Headers.Set(StompHeaderNames.HeartBeat, new AsciiString("5000,5000"));
            connFrame.Headers.Set(new AsciiString("Authorization"), new AsciiString(Authorization));
            channel.WriteAndFlushAsync(connFrame);
        }

        /// <inheritdoc />
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            try
            {
                case StompCommand.CONNECTED:
                    IStompFrame subscribeFrame = new DefaultStompFrame(StompCommand.SUBSCRIBE);
                    subscribeFrame.Headers.Set(StompHeaderNames.Destination, new AsciiString(Topic));
                    subscribeFrame.Headers.Set(StompHeaderNames.Receipt, new AsciiString(subscrReceiptId));
                    subscribeFrame.Headers.Set(StompHeaderNames.Id, new AsciiString("1"));
                    Console.WriteLine("connected, sending subscribe frame: " + subscribeFrame);
                    this._state = ClientState.AUTHENTICATED;
                    ctx.WriteAndFlushAsync(subscribeFrame);
                    ctx.Channel.Pipeline.Replace("idle", "idle",
                        new IdleStateHandler(5000, 5000, 0));
                    break;

                default:
                    break;
            }
        }
    }
}
