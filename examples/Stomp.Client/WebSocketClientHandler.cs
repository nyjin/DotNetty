﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Codecs.Http;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Common.Concurrency;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class WebSocketClientHandler : SimpleChannelInboundHandler<object>
    {
        readonly WebSocketClientHandshaker handshaker;
        readonly TaskCompletionSource completionSource;

        public WebSocketClientHandler(WebSocketClientHandshaker handshaker)
        {
            this.handshaker = handshaker;
            this.completionSource = new TaskCompletionSource();
        }

        public Task HandshakeCompletion => this.completionSource.Task;

        public override void ChannelActive(IChannelHandlerContext ctx) => 
            this.handshaker.HandshakeAsync(ctx.Channel);

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            Console.WriteLine("WebSocket Client disconnected!");
        }

        protected override void ChannelRead0(IChannelHandlerContext ctx, object msg)
        {
            IChannel ch = ctx.Channel;
            if (!this.handshaker.IsHandshakeComplete)
            {
                try
                {
                    this.handshaker.FinishHandshake(ch, (IFullHttpResponse)msg);
                    Console.WriteLine("WebSocket Client connected!");

                    this.completionSource.TryComplete();
                }
                catch (WebSocketHandshakeException e)
                {
                    Console.WriteLine("WebSocket Client failed to connect");
                    this.completionSource.TrySetException(e);
                }

                return;
            }

            if (msg is IFullHttpResponse response)
            {
                throw new InvalidOperationException(
                    $"Unexpected FullHttpResponse (getStatus={response.Status}, content={response.Content.ToString(Encoding.UTF8)})");
            }

            if (msg is PongWebSocketFrame)
            {
                Console.WriteLine("WebSocket Client received pong");
                return;
            }
            else if (msg is CloseWebSocketFrame)
            {
                Console.WriteLine("WebSocket Client received closing");
                ch.CloseAsync();
                return;
            }

            else if (msg is TextWebSocketFrame textWebSocketFrame)
            {
                if (textWebSocketFrame.Content.ReadableBytes == 1)
                {
                    Console.WriteLine("Heartbeat received");
                    return;
                }
                else
                {
                    ctx.FireChannelRead(textWebSocketFrame.Retain());
                }
                
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            this.completionSource.TrySetException(exception);
            ctx.CloseAsync();
        }
    }
}
