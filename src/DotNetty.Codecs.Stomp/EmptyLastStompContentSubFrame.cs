﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using System;
    using DotNetty.Buffers;
    using DotNetty.Common;

    public sealed class EmptyLastStompContentSubFrame : ILastStompContentSubFrame
    {
        public static readonly EmptyLastStompContentSubFrame Default = new EmptyLastStompContentSubFrame();

        EmptyLastStompContentSubFrame()
        {
            this.Content = Unpooled.Empty;
        }

        public DecoderResult Result
        {
            get => DecoderResult.Success;
            set => throw new NotSupportedException("read only");
        }

        public int ReferenceCount => 1;

        public IReferenceCounted Retain() => this;

        public IReferenceCounted Retain(int increment) => this;

        public IReferenceCounted Touch() => this;

        public IReferenceCounted Touch(object hint) => this;

        public bool Release() => false;

        public bool Release(int decrement) => false;

        public IByteBuffer Content { get; }

        public IByteBufferHolder Copy() => this;

        public IByteBufferHolder Duplicate() => this;

        public IByteBufferHolder RetainedDuplicate() => this;

        public IByteBufferHolder Replace(IByteBuffer content) => new DefaultLastStompContentSubFrame(content);

        public override string ToString() => nameof(EmptyLastStompContentSubFrame);
    }
}