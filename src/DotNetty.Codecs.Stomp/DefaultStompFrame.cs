// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using System;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common;

    public class DefaultStompFrame : DefaultStompHeadersSubFrame, IStompFrame
    {
        public DefaultStompFrame(StompCommand command) 
            : this(command, Unpooled.Buffer(0))
        {
            
        }

        public DefaultStompFrame(StompCommand command, IByteBuffer content)
            : this(command, content, null)
        {
            
        }

        DefaultStompFrame(StompCommand command, IByteBuffer content, DefaultStompHeaders headers)
            : base(command, headers)
        {
            this.Content = content ?? throw new NullReferenceException("content");
        }

        public int ReferenceCount => this.Content.ReferenceCount;

        public IReferenceCounted Retain()
        {
            this.Content.Retain();
            return this;
        }

        public IReferenceCounted Retain(int increment)
        {
            this.Content.Retain(increment);
            return this;
        }

        public IReferenceCounted Touch()
        {
            this.Content.Touch();
            return this;
        }

        public IReferenceCounted Touch(object hint)
        {
            this.Content.Touch(hint);
            return this;
        }

        public bool Release()
        {
            return this.Content.Release();
        }

        public bool Release(int decrement)
        {
            return this.Content.Release(decrement);
        }

        public IByteBuffer Content { get; }

        public IByteBufferHolder Copy()
        {
            return this.Replace(this.Content.Copy());
        }

        public IByteBufferHolder Duplicate()
        {
            return this.Replace(this.Content.Duplicate());
        }

        public IByteBufferHolder RetainedDuplicate()
        {
            return this.Replace(this.Content.RetainedDuplicate());
        }

        public IByteBufferHolder Replace(IByteBuffer content)
        {
            return new DefaultStompFrame(this.Command, this.Content, (DefaultStompHeaders)((DefaultStompHeaders)this.Headers).Copy());
        }

        public override string ToString()
        {
            return "DefaultStompFrame{" +
                "command=" + this.Command +
                ", headers=" + this.Headers +
                ", content=" + this.Content.ToString(Encoding.UTF8) +
                '}';
        }
    }
}