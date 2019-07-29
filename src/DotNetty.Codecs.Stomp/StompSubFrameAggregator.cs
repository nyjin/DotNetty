// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using System;
    using DotNetty.Buffers;
    using DotNetty.Transport.Channels;

    public class StompSubFrameAggregator : MessageAggregator<IStompSubFrame, IStompHeadersSubFrame, IStompContentSubFrame, IStompFrame>
    {
        public StompSubFrameAggregator(int maxContentLength)
            : base(maxContentLength)
        {
        }

        protected override bool IsStartMessage(IStompSubFrame msg)
        {
            return msg is IStompHeadersSubFrame;
        }

        protected override bool IsContentMessage(IStompSubFrame msg)
        {
            return msg is IStompContentSubFrame;
        }

        protected override bool IsLastContentMessage(IStompContentSubFrame msg)
        {
            return msg is ILastStompContentSubFrame;
        }

        protected override bool IsAggregated(IStompSubFrame msg)
        {
            return msg is IStompFrame;
        }

        protected override bool IsContentLengthInvalid(IStompHeadersSubFrame start, int maxContentLength)
        {
            return Math.Min(Int32.MaxValue, start.Headers.GetLong(StompHeaderNames.ContentLength, -1)) > maxContentLength;
        }

        protected override object NewContinueResponse(IStompHeadersSubFrame start, int maxContentLength, IChannelPipeline pipeline)
        {
            return null;
        }

        protected override bool CloseAfterContinueResponse(object msg)
        {
            throw new System.NotSupportedException();
        }

        protected override bool IgnoreContentAfterContinueResponse(object msg)
        {
            throw new System.NotSupportedException();
        }

        protected override IStompFrame BeginAggregation(IStompHeadersSubFrame start, IByteBuffer content)
        {
            IStompFrame ret = new DefaultStompFrame(start.Command, content);
            ret.Headers.Set(start.Headers);
            return ret;
        }
    }
}