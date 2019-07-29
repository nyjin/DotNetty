// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using DotNetty.Buffers;

    public class DefaultLastStompContentSubFrame : DefaultStompContentSubframe, ILastStompContentSubFrame
    {
        public DefaultLastStompContentSubFrame(IByteBuffer content)
            : base(content)
        {
        }

        public override IByteBufferHolder Replace(IByteBuffer content)
        {
            return new DefaultLastStompContentSubFrame(content);
        }

        public override string ToString()
        {
            return "DefaultLastStompContent{" +
                "decoderResult=" + this.Result +
                '}';
        }
    }
}