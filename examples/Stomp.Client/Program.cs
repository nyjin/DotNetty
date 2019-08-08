using System;

namespace Stomp.Client
{
    using System.Net;
    using System.Net.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Http.WebSockets.Extensions.Compression;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Common.Utilities;
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
                         .WriteTo.File("logs\\app.log", LogEventLevel.Debug, rollingInterval: RollingInterval.Hour)
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
                        true,
                        new DefaultHttpHeaders()));

                bootstrap.Handler(
                    new ActionChannelInitializer<IChannel>(
                        channel =>
                        {
                            IChannelPipeline pipeline = channel.Pipeline;
                            var tlsHandler = 
                                new TlsHandler(stream => new SslStream(stream, true, (sender, certificate, chain, errors) => true), new ClientTlsSettings(uri.AbsoluteUri));

                            pipeline.AddLast("idle", new IdleStateHandler(0, 0, 0));
                            //pipeline.AddLast("client tls", tlsHandler);
                            pipeline.AddLast("logging", new LoggingHandler());
                            pipeline.AddLast("http codec", new HttpClientCodec());
                            pipeline.AddLast("http aggregator", new HttpObjectAggregator(8192));
                            //pipeline.AddLast("websocket handler", WebSocketClientCompressionHandler.Instance);
                            pipeline.AddLast(
                                "websocket handler",
                                webSocketHandler);
                            pipeline.AddLast("websocket frame aggregator", new WebSocketFrameAggregator(65536));
                            //pipeline.AddLast("websocket client handler", webSocketHandler);
                            pipeline.AddLast("stomp-decoder", new StompSubFrameDecoder());
                            pipeline.AddLast("stomp-encoder", new StompSubFrameEncoder());
                            pipeline.AddLast("aggregator", new StompSubFrameAggregator(1048576));
                            pipeline.AddLast("handler", stompHandler);
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

                    //if (msg == null)
                    //{
                    //    break;
                    //}
                    //else if ("bye".Equals(msg.ToLower()))
                    //{
                    //    await ch.WriteAndFlushAsync(new CloseWebSocketFrame());
                    //    break;
                    //}
                    //else if ("ping".Equals(msg.ToLower()))
                    //{
                    //    var frame = new PingWebSocketFrame(
                    //        Unpooled.WrappedBuffer(
                    //            new byte[]
                    //            {
                    //                8, 1, 8, 1
                    //            }));

                    //    await ch.WriteAndFlushAsync(frame);
                    //}
                    //else
                    //{
                    //    WebSocketFrame frame = new TextWebSocketFrame(msg);
                    //    await ch.WriteAndFlushAsync(frame);
                    //}
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

    /**
     * STOMP client inbound handler implementation, which just passes received messages to listener
     */
    public class StompClientHandler : SimpleChannelInboundHandler<IStompFrame>
    {
        public string Host { get; }

        public string Login { get; }

        public string Passcode { get; }

        public string Topic { get; }

        public string Authorization { get; }

        /// <inheritdoc />
        public StompClientHandler(string host, string login, string passcode, string topic, string auth)
        {
            this.Host = host;
            this.Login = login;
            this.Passcode = passcode;
            this.Topic = topic;
            this.Authorization = auth;
        }

        private enum ClientState
        {
            AUTHENTICATING,
            AUTHENTICATED,
            SUBSCRIBED,
            DISCONNECTING
        }

        private ClientState _state;

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

            //this._state = ClientState.AUTHENTICATING;
            //IStompFrame connFrame = new DefaultStompFrame(StompCommand.CONNECT);
            //connFrame.Headers.Set(StompHeaderNames.AcceptVersion, new AsciiString("1.0,1.1,1.2"));
            ////connFrame.Headers.Set(StompHeaderNames.Host, new AsciiString(Host));
            //connFrame.Headers.Set(StompHeaderNames.Login, new AsciiString(Login));
            //connFrame.Headers.Set(StompHeaderNames.PassCode, new AsciiString(Passcode));
            //connFrame.Headers.Set(StompHeaderNames.HeartBeat, new AsciiString("5000,5000"));
            //connFrame.Headers.Set(new AsciiString("Authorization"), new AsciiString(Authorization));
            //context.WriteAndFlushAsync(connFrame);


        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, IStompFrame msg)
        {
            string subscrReceiptId = "001";
            string disconReceiptId = "002";

            switch (msg.Command)
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

            //case StompCommand.RECEIPT:
            //    var receiptHeader = subscribeFrame.Headers.Get(StompHeaderNames.ReceiptId, new AsciiString(string.Empty));
            //    if (state == ClientState.AUTHENTICATED && receiptHeader.ContentEquals(new AsciiString(subscrReceiptId)))
            //    {
            //        StompFrame msgFrame = new DefaultStompFrame(StompCommand.SEND);
            //        msgFrame.Headers.set(StompHeaders.DESTINATION, StompClient.TOPIC);
            //        msgFrame.content().writeBytes("some payload".getBytes());
            //        System.out.println("subscribed, sending message frame: " + msgFrame);
            //        state = ClientState.SUBSCRIBED;
            //        ctx.writeAndFlush(msgFrame);
            //    } else if (state == ClientState.DISCONNECTING && receiptHeader.equals(disconReceiptId)) {
            //        System.out.println("disconnected");
            //        ctx.close();
            //    } else {
            //        throw new IllegalStateException("received: " + frame + ", while internal state is " + state);
            //    }
            //    break;
            //case MESSAGE:
            //    if (state == ClientState.SUBSCRIBED) {
            //        System.out.println("received frame: " + frame);
            //        StompFrame disconnFrame = new DefaultStompFrame(StompCommand.DISCONNECT);
            //        disconnFrame.Headers.set(StompHeaders.RECEIPT, disconReceiptId);
            //        System.out.println("sending disconnect frame: " + disconnFrame);
            //        state = ClientState.DISCONNECTING;
            //        ctx.writeAndFlush(disconnFrame);
            //    }
            //    break;
            //default:
            //    break;
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            //this.completionSource.TrySetException(exception);
            ctx.CloseAsync();
        }
    }
}
