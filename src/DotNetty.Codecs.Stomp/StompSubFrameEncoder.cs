// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Stomp
{
    using System.Collections.Generic;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class StompSubFrameEncoder : MessageToMessageEncoder<IStompSubFrame>
    {
        protected override void Encode(IChannelHandlerContext context, IStompSubFrame message, List<object> output)
        {
            if (message is IStompFrame)
            {
                IStompFrame frame = (IStompFrame)message;
                IByteBuffer frameBuf = EncodeFrame(frame, context);
                output.Add(frameBuf);
                IByteBuffer contentBuf = EncodeContent(frame, context);
                output.Add(contentBuf);
            }
            else if (message is IStompHeadersSubFrame)
            {
                IStompHeadersSubFrame frame = (IStompHeadersSubFrame)message;
                IByteBuffer buf = EncodeFrame(frame, context);
                output.Add(buf);
            }
            else if (message is IStompContentSubFrame)
            {
                IStompContentSubFrame stompContentSubFrame = (IStompContentSubFrame)message;
                IByteBuffer buf = EncodeContent(stompContentSubFrame, context);
                output.Add(buf);
            }
        }

        static IByteBuffer EncodeContent(IStompContentSubFrame content, IChannelHandlerContext context)
        {
            if (content is ILastStompContentSubFrame)
            {
                IByteBuffer buf = context.Allocator.Buffer(content.Content.ReadableBytes + 1);
                buf.WriteBytes(content.Content);
                buf.WriteByte(StompConstants.Null);
                return buf;
            }
            else
            {
                return (IByteBuffer)content.Content.Retain();
            }
        }

        static IByteBuffer EncodeFrame(IStompHeadersSubFrame frame, IChannelHandlerContext context)
        {
            IByteBuffer buf = context.Allocator.Buffer();

            buf.WriteCharSequence(new StringCharSequence(frame.Command.ToString()), Encoding.ASCII);
            buf.WriteByte(StompConstants.LF);
            AsciiHeadersEncoder headersEncoder = new AsciiHeadersEncoder(buf, AsciiHeadersEncoder.SeparatorType.Colon, AsciiHeadersEncoder.NewlineType.LF);
            foreach (HeaderEntry<ICharSequence, ICharSequence> entry in frame.Headers)
            {
                headersEncoder.Encode(entry);
            }

            buf.WriteByte(StompConstants.LF);
            return buf;
        }
    }
}