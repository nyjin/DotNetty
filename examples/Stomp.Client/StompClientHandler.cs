// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System;
    using System.Text;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Utilities;
    using DotNetty.Handlers.Timeout;
    using DotNetty.Transport.Channels;

    /**
     * STOMP client inbound handler implementation, which just passes received messages to listener
     */
    public class StompClientHandler : SimpleChannelInboundHandler<IStompFrame>
    {
        public IChannel Channel { get; set; }

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

        public void ActiveStomp(IChannel ch)
        {
            IStompFrame connFrame = new DefaultStompFrame(StompCommand.CONNECT);
            connFrame.Headers.Set(StompHeaderNames.AcceptVersion, new AsciiString("1.0,1.1,1.2"));
            //connFrame.Headers.Set(StompHeaderNames.Host, new AsciiString(Host));
            connFrame.Headers.Set(StompHeaderNames.Login, new AsciiString(this.Login));
            connFrame.Headers.Set(StompHeaderNames.PassCode, new AsciiString(this.Passcode));
            connFrame.Headers.Set(StompHeaderNames.HeartBeat, new AsciiString("5000,5000"));
            connFrame.Headers.Set(new AsciiString("Authorization"), new AsciiString(this.Authorization));

            ch.WriteAndFlushAsync(connFrame);
        }

        /// <inheritdoc />
        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);

            this._state = ClientState.AUTHENTICATING;

            IStompFrame connFrame = new DefaultStompFrame(StompCommand.CONNECT);
            connFrame.Headers.Set(StompHeaderNames.AcceptVersion, new AsciiString("1.0,1.1,1.2"));
            //connFrame.Headers.Set(StompHeaderNames.Host, new AsciiString(Host));
            connFrame.Headers.Set(StompHeaderNames.Login, new AsciiString(this.Login));
            connFrame.Headers.Set(StompHeaderNames.PassCode, new AsciiString(this.Passcode));
            connFrame.Headers.Set(StompHeaderNames.HeartBeat, new AsciiString("5000,5000"));
            connFrame.Headers.Set(new AsciiString("Authorization"), new AsciiString(this.Authorization));
            context.WriteAndFlushAsync(connFrame);
            
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, IStompFrame msg)
        {
            string subscrReceiptId = "001";
            string disconReceiptId = "002";

            switch (msg.Command)
            {
                case StompCommand.CONNECTED:
                    IStompFrame subscribeFrame = new DefaultStompFrame(StompCommand.SUBSCRIBE);
                    subscribeFrame.Headers.Set(StompHeaderNames.Destination, new AsciiString(this.Topic));
                    subscribeFrame.Headers.Set(StompHeaderNames.Receipt, new AsciiString(subscrReceiptId));
                    subscribeFrame.Headers.Set(StompHeaderNames.Id, new AsciiString("1"));
                    Console.WriteLine("connected, sending subscribe frame: " + subscribeFrame);
                    this._state = ClientState.AUTHENTICATED;
                    ctx.WriteAndFlushAsync(subscribeFrame);

                    this.Channel.Pipeline.Replace("idle", "idle",
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