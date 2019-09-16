using System;

namespace Stomp.Client
{
    using System.IO;
    using System.Net;
    using System.Net.Security;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using DotNetty.Buffers;
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
    using Newtonsoft.Json;
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

            var result = GetSubscriptionInfo("subscription.json");

            var auth = result.Authorization;
            var id = result.SubscriptionInfo.Login;
            var passcode = result.SubscriptionInfo.Passcode;
            var topic = result.SubscriptionInfo.TopicName;

            var uri = new Uri("ws://172.19.136.161:30239/stomp");
            var port = 30239;

            //var uri = new Uri("wss://op-lime-pub.ncsoft.com/stomp");
            //var port = 443;

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

        static LimeLoginResult GetSubscriptionInfo(string jsonFile)
        {
            Newtonsoft.Json.JsonSerializer serializer = new JsonSerializer();

            using (var streamReader = new StreamReader(jsonFile))
            {
                using(var jsonReader = new JsonTextReader(streamReader))
                {
                    return serializer.Deserialize<LimeLoginResult>(jsonReader);
                }
            }

            
        }
    }

    /// <summary>
    /// Websocket 서버 정보
    /// </summary>
    public class SubscriptionInfo
    {
        /// <summary>
        /// STOMP Topic 구독 정보
        /// </summary>
        public string TopicName { get; set; }

        /// <summary>
        /// STOMP 서버 정보
        /// </summary>
        public string SubscribeUrl { get; set; }

        /// <summary>
        /// STOMP 서버 연결에 사용할 login
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// STOMP 서버 연결에 사용할 passcode
        /// </summary>
        public string Passcode { get; set; }

        /// <summary>
        /// STOMP 전송 시 보낼 곳
        /// </summary>
        public string ServerAppDest { get; set; }
    }

    /// <summary>
    /// 로그인 요청 결과
    /// </summary>
    public class LimeLoginResult
    {
        /// <summary>
        /// Websocket 서버 정보
        /// </summary>
        public SubscriptionInfo SubscriptionInfo { get; set; }

        /// <summary>
        /// 기능 정보
        /// </summary>
        [JsonProperty(PropertyName = "feature")]
        public FeatureInfo FeatureInfo { get; set; }

        public string Authorization { get; set; }
    }

    /// <summary>
    /// 기능 정보
    /// </summary>
    public class FeatureInfo
    {
        /// <summary>
        /// 토픽 구독 여부
        /// </summary>
        public bool EnableSubscribeTopic { get; set; }

        /// <summary>
        /// 그룹 생성 비활성화
        /// </summary>
        public bool DisableCreateGroup { get; set; }

        /// <summary>
        /// 채널 생성 비활성화
        /// </summary>
        public bool DisableCreateChannel { get; set; }
    }
}
