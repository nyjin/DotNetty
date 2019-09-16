// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Utilities;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Transport.Channels;

    public class HeartBeatClientHandler : ChannelHandlerAdapter
    {
        readonly StompClientHandler stompHandler;
        readonly byte[] heartbeatBytes = Encoding.UTF8.GetBytes("\n");

        public HeartBeatClientHandler(StompClientHandler stompHandler)
        {
            this.stompHandler = stompHandler;
        }

        /// <inheritdoc />
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            if (evt is IdleStateEvent idleEvent)
            {
                if (idleEvent.State == IdleState.WriterIdle)
                {

                    //IStompFrame connFrame = new DefaultStompFrame(StompCommand.CONNECT);
                    //connFrame.Headers.Set(StompHeaderNames.AcceptVersion, new AsciiString("1.0,1.1,1.2"));
                    //connFrame.Headers.Set(StompHeaderNames.Host, new AsciiString(Host));
                    //connFrame.Headers.Set(StompHeaderNames.Login, new AsciiString(stompHandler.Login));
                    //connFrame.Headers.Set(StompHeaderNames.PassCode, new AsciiString(stompHandler.Passcode));
                    //connFrame.Headers.Set(StompHeaderNames.HeartBeat, new AsciiString("5000,5000"));
                    var buff = context.Allocator.Buffer();
                    buff.WriteString("\n", Encoding.UTF8);
                    var textFrame = new TextWebSocketFrame(buff);
                    var pingFrame = new PingWebSocketFrame();
                    //IStompFrame connFrame = new DefaultStompFrame(StompCommand.UNKNOWN, buff);
                    context.WriteAndFlushAsync(textFrame);
                    
                    //connFrame.Headers.Set(new AsciiString("Authorization"), new AsciiString(stompHandler.Authorization));
                    //context.WriteAndFlushAsync(connFrame);

                    //var buff = Unpooled.Buffer(2);
                    //buff.WriteBytes(this.heartbeatBytes);
                    //context.WriteAndFlushAsync(new PingWebSocketFrame(buff));
                }

                //if (idleEvent.State == IdleState.ReaderIdle)
                //{
                //    var buff = Unpooled.Buffer(2);
                //    buff.WriteBytes(this.heartbeatBytes);
                //    context.WriteAndFlushAsync(new PingWebSocketFrame(buff));
                //    //this.CloseAsync(context);
                //}
            }
            base.UserEventTriggered(context, evt);
        }
    }
}