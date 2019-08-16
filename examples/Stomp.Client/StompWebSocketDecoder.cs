// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Transport.Channels;

    public class StompWebSocketDecoder : MessageToMessageDecoder<WebSocketFrame>
    {
        Encoding encoding;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StringEncoder" /> class with the specified character
        /// </summary>
        /// <param name="encoding">Encoding.</param>
        public StompWebSocketDecoder(Encoding encoding)
        {
            this.encoding = encoding ?? throw new NullReferenceException("encoding");
        }

        /// <inheritdoc />
        protected override void Decode(IChannelHandlerContext context, WebSocketFrame message, List<object> output)
        {
            if (!(message is TextWebSocketFrame textFrame))
            {
                return;
            }

            string rawContent = textFrame.Text();
            IByteBuffer buff = ByteBufferUtil.WriteUtf8(context.Allocator, rawContent);
            output.Add(buff);
        }
    }
}