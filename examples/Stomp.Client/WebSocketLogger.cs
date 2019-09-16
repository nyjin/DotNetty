// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Common.Internal.Logging;
    using DotNetty.Handlers.Logging;
    using DotNetty.Transport.Channels;

    public class WebSocketLogger : ChannelHandlerAdapter
    {
        const LogLevel DefaultLevel = LogLevel.DEBUG;

        protected readonly InternalLogLevel InternalLevel;
        protected readonly IInternalLogger Logger;

        /// <summary>
        ///     Creates a new instance whose logger name is the fully qualified class
        ///     name of the instance with hex dump enabled.
        /// </summary>
        public WebSocketLogger()
            : this(DefaultLevel)
        {
        }

        /// <summary>
        ///     Creates a new instance whose logger name is the fully qualified class
        ///     name of the instance
        /// </summary>
        /// <param name="level">the log level</param>
        public WebSocketLogger(LogLevel level)
            : this(typeof(LoggingHandler), level)
        {
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name and with hex dump
        ///     enabled
        /// </summary>
        /// <param name="type">the class type to generate the logger for</param>
        public WebSocketLogger(Type type)
            : this(type, DefaultLevel)
        {
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name.
        /// </summary>
        /// <param name="type">the class type to generate the logger for</param>
        /// <param name="level">the log level</param>
        public WebSocketLogger(Type type, LogLevel level)
        {
            if (type == null)
            {
                throw new NullReferenceException("type");
            }

            this.Logger = InternalLoggerFactory.GetInstance(type);
            this.Level = level;
            this.InternalLevel = level.ToInternalLevel();
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name.
        /// </summary>
        /// <param name="name">the name of the class to use for the logger</param>
        /// <param name="level">the log level</param>
        public WebSocketLogger(string name, LogLevel level)
        {
            if (name == null)
            {
                throw new NullReferenceException("name");
            }

            this.Logger = InternalLoggerFactory.GetInstance(name);
            this.Level = level;
            this.InternalLevel = level.ToInternalLevel();
        }

        /// <summary>
        ///     Creates a new instance with the specified logger name using the default log level.
        /// </summary>
        /// <param name="name">the name of the class to use for the logger</param>
        public WebSocketLogger(string name)
            : this(name, DefaultLevel)
        {
        }

        /// <summary>
        ///     Returns the <see cref="LogLevel" /> that this handler uses to log
        /// </summary>
        public LogLevel Level { get; }

        public override bool IsSharable => true;

        public override void ChannelRead(IChannelHandlerContext ctx, object message)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "RECEIVED", message));
            }
            ctx.FireChannelRead(message);
        }

        string Format(IChannelHandlerContext ctx, string eventName, object message)
        {
            if (message is TextWebSocketFrame textFrame)
            {
                return this.FormatTextFrame(eventName, textFrame);
            }
            else if (message is WebSocketFrame webSocketFrame)
            {
                return this.FormatSimple(eventName, webSocketFrame);
            }

            return eventName;
        }

        StringBuilder GetFrameBuffer(WebSocketFrame webSocketFrame)
        {
            var length = webSocketFrame.Content.ReadableBytes > 0 ? webSocketFrame.Content.ReadableBytes : webSocketFrame.Content.WritableBytes;
            return new StringBuilder(length + 20);
        }

        string FormatSimple(string eventName, WebSocketFrame webSocketFrame)
        {
            var buf = GetFrameBuffer(webSocketFrame);

            buf.Append(eventName).Append(": ").Append(webSocketFrame.GetType()).Append('\n');

            return buf.ToString();
        }

        string FormatTextFrame(string eventName, TextWebSocketFrame textFrame)
        {
            var length = textFrame.Content.ReadableBytes > 0 ? textFrame.Content.ReadableBytes : textFrame.Content.WritableBytes;
            var buf = new StringBuilder(length + 20);

            buf.Append(eventName).Append(": ").Append("TextFrame").Append('\n')
               .Append(textFrame.Text());

            return buf.ToString();
        }

        public override void ChannelReadComplete(IChannelHandlerContext ctx)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "RECEIVED_COMPLETE"));
            }
            ctx.FireChannelReadComplete();
        }

        string Format(IChannelHandlerContext ctx, string eventName)
        {
            return eventName;
        }

        public override Task WriteAsync(IChannelHandlerContext ctx, object msg)
        {
            if (this.Logger.IsEnabled(this.InternalLevel))
            {
                this.Logger.Log(this.InternalLevel, this.Format(ctx, "WRITE", msg));
            }
            return ctx.WriteAsync(msg);
        }
    }
}