// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Codecs;
    using DotNetty.Codecs.Http.WebSockets;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Transport.Channels;

    public class StompWebSocketEncoder : MessageToMessageEncoder<IStompSubFrame>
    {
        readonly StompSubFramePublicEncoder encoder = new StompSubFramePublicEncoder();

        /// <inheritdoc />
        protected override void Encode(IChannelHandlerContext context, IStompSubFrame message, List<object> output)
        {
            Contract.Requires(context != null);
            Contract.Requires(message != null);
            Contract.Requires(output != null);

            var outputList = new List<object>();
            this.encoder.Encode(context, message, outputList);

            CompositeByteBuffer buff = context.Allocator.CompositeBuffer().AddComponents(outputList.Cast<IByteBuffer>());
            string rawContent = buff.ToString(0, buff.WritableBytes, Encoding.UTF8);
            TextWebSocketFrame frame = new TextWebSocketFrame(rawContent);
            output.Add(frame);
        }
    }
}