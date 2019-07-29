// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs.Stomp
{
    using System;
    using System.Collections.Generic;
    using DotNetty.Buffers;
    using DotNetty.Common.Internal;
    using DotNetty.Common.Utilities;
    using DotNetty.Transport.Channels;

    public class StompSubFrameDecoder : ReplayingDecoder<StompSubFrameDecoder.ParseState>
    {
        const int DefaultChunkSize = 8132;
        const int DefaultMaxLineLength = 1024;

        public enum ParseState
        {
            SkipControlCharacters,
            ReadHeaders,
            ReadContent,
            FinalizeFrameRead,
            BadFrame,
            InvalidChunk
        }

        readonly int maxLineLength;
        readonly int maxChunkSize;
        readonly bool validateHeaders;
        int alreadyReadChunkSize;
        ILastStompContentSubFrame lastContent;
        long contentLength = -1;

        public StompSubFrameDecoder()
            : this(DefaultMaxLineLength, DefaultChunkSize)
        {
        }

        public StompSubFrameDecoder(bool validateHeaders)
            : this(DefaultMaxLineLength, DefaultChunkSize, validateHeaders)
        {
        }

        public StompSubFrameDecoder(int maxLineLength, int maxChunkSize)
            : this(maxLineLength, maxChunkSize, false)
        {
        }

        public StompSubFrameDecoder(int maxLineLength, int maxChunkSize, bool validateHeaders)
            : base(ParseState.SkipControlCharacters)
        {
            if (maxLineLength <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maxLineLength must be a positive integer: " +
                    maxLineLength);
            }

            if (maxChunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    "maxChunkSize must be a positive integer: " +
                    maxChunkSize);
            }

            this.maxChunkSize = maxChunkSize;
            this.maxLineLength = maxLineLength;
            this.validateHeaders = validateHeaders;
        }

        static void SkipControlCharacters(IByteBuffer buffer)
        {
            for (;;)
            {
                byte b = buffer.ReadByte();
                if (b != StompConstants.CR && b != StompConstants.LF)
                {
                    buffer.SetReaderIndex(buffer.ReaderIndex - 1);
                    break;
                }
            }
        }

        void InvalidLineLength()
        {
            throw new TooLongFrameException("An STOMP line is larger than " + this.maxLineLength + " bytes.");
        }

        string ReadLine(IByteBuffer buffer, int initialBufferSize)
        {
            AppendableCharSequence buf = new AppendableCharSequence(initialBufferSize);
            int lineLength = 0;
            for (;;)
            {
                byte nextByte = buffer.ReadByte();
                if (nextByte == StompConstants.CR)
                {
                    //do nothing
                }
                else if (nextByte == StompConstants.LF)
                {
                    return buf.ToString();
                }
                else
                {
                    if (lineLength >= this.maxLineLength)
                    {
                        this.InvalidLineLength();
                    }

                    lineLength++;
                    buf.Append((char)nextByte);
                }
            }
        }

        StompCommand ReadCommand(IByteBuffer input)
        {
            string commandStr = this.ReadLine(input, 16);

            if (!Enum.TryParse(commandStr, out StompCommand command))
            {
                commandStr = commandStr.ToUpper();
            }

            if (!Enum.TryParse(commandStr, out command))
            {
                throw new DecoderException("failed to read command from channel");
            }

            return command;
        }

        static long GetContentLength(IStompHeaders headers, long defaultValue)
        {
            long contentLength = headers.GetLong(StompHeaderNames.ContentLength, defaultValue);
            if (contentLength < 0)
            {
                throw new DecoderException(StompHeaderNames.ContentLength + " must be non-negative");
            }

            return contentLength;
        }

        ParseState ReadHeaders(IByteBuffer buffer, IStompHeaders headers)
        {
            AppendableCharSequence buf = new AppendableCharSequence(128);
            for (;;)
            {
                bool headerRead = this.ReadHeader(headers, buf, buffer);
                if (!headerRead)
                {
                    if (headers.Contains(StompHeaderNames.ContentLength))
                    {
                        this.contentLength = GetContentLength(headers, 0);
                        if (this.contentLength == 0)
                        {
                            return ParseState.FinalizeFrameRead;
                        }
                    }

                    return ParseState.ReadContent;
                }
            }
        }

        bool ReadHeader(IStompHeaders headers, AppendableCharSequence buf, IByteBuffer buffer)
        {
            buf.Reset();
            int lineLength = 0;
            string key = null;
            bool valid = false;
            for (;;)
            {
                byte nextByte = buffer.ReadByte();

                if (nextByte == StompConstants.Colon && key == null)
                {
                    key = buf.ToString();
                    valid = true;
                    buf.Reset();
                }
                else if (nextByte == StompConstants.CR)
                {
                    //do nothing
                }
                else if (nextByte == StompConstants.LF)
                {
                    if (key == null && lineLength == 0)
                    {
                        return false;
                    }
                    else if (valid)
                    {
                        headers.Add(new StringCharSequence(key), new StringCharSequence(buf.ToString()));
                    }
                    else if (this.validateHeaders)
                    {
                        this.InvalidHeader(key, buf.ToString());
                    }

                    return true;
                }
                else
                {
                    if (lineLength >= this.maxLineLength)
                    {
                        this.InvalidLineLength();
                    }

                    if (nextByte == StompConstants.Colon && key != null)
                    {
                        valid = false;
                    }

                    lineLength++;
                    buf.Append((char)nextByte);
                }
            }
        }

        void InvalidHeader(string key, string value)
        {
            string line = key != null ? key + ":" + value : value;
            throw new ArgumentException(
                "a header value or name contains a prohibited character ':'"
                + ", " + line);
        }

        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            switch (this.State)
            {
                case ParseState.SkipControlCharacters:
                    SkipControlCharacters(input);
                    this.Checkpoint(ParseState.ReadHeaders);
                    goto case ParseState.ReadHeaders;
                // Fall through.
                case ParseState.ReadHeaders:
                    StompCommand command = StompCommand.UNKNOWN;
                    IStompHeadersSubFrame frame = null;
                    try
                    {
                        command = this.ReadCommand(input);
                        frame = new DefaultStompHeadersSubFrame(command);
                        this.Checkpoint(this.ReadHeaders(input, frame.Headers));
                        output.Add(frame);
                    }
                    catch (Exception e)
                    {
                        if (frame == null)
                        {
                            frame = new DefaultStompHeadersSubFrame(command);
                        }

                        frame.Result = DecoderResult.Failure(e);
                        output.Add(frame);
                        this.Checkpoint(ParseState.BadFrame);
                        return;
                    }

                    break;
                case ParseState.BadFrame:
                    input.SkipBytes(this.ActualReadableBytes);
                    return;
            }

            try
            {
                switch (this.State)
                {
                    case ParseState.ReadContent:
                        int toRead = input.ReadableBytes;
                        if (toRead == 0)
                        {
                            return;
                        }

                        if (toRead > this.maxChunkSize)
                        {
                            toRead = this.maxChunkSize;
                        }

                        if (this.contentLength >= 0)
                        {
                            int remainingLength = (int)(this.contentLength - this.alreadyReadChunkSize);
                            if (toRead > remainingLength)
                            {
                                toRead = remainingLength;
                            }

                            IByteBuffer chunkBuffer = ByteBufferUtil.ReadBytes(context.Allocator, input, toRead);
                            if ((this.alreadyReadChunkSize += toRead) >= this.contentLength)
                            {
                                this.lastContent = new DefaultLastStompContentSubFrame(chunkBuffer);
                                this.Checkpoint(ParseState.FinalizeFrameRead);
                            }
                            else
                            {
                                output.Add(new DefaultStompContentSubframe(chunkBuffer));
                                return;
                            }
                        }
                        else
                        {
                            int nulIndex = ByteBufferUtil.IndexOf(input, input.ReaderIndex, input.WriterIndex, StompConstants.Null);
                            if (nulIndex == input.ReaderIndex)
                            {
                                this.Checkpoint(ParseState.FinalizeFrameRead);
                            }
                            else
                            {
                                if (nulIndex > 0)
                                {
                                    toRead = nulIndex - input.ReaderIndex;
                                }
                                else
                                {
                                    toRead = input.WriterIndex - input.ReaderIndex;
                                }

                                IByteBuffer chunkBuffer = ByteBufferUtil.ReadBytes(context.Allocator, input, toRead);
                                this.alreadyReadChunkSize += toRead;
                                if (nulIndex > 0)
                                {
                                    this.lastContent = new DefaultLastStompContentSubFrame(chunkBuffer);
                                    this.Checkpoint(ParseState.FinalizeFrameRead);
                                }
                                else
                                {
                                    output.Add(new DefaultStompContentSubframe(chunkBuffer));
                                    return;
                                }
                            }
                        }

                        goto case ParseState.FinalizeFrameRead;
                    // Fall through.
                    case ParseState.FinalizeFrameRead:
                        SkipNullCharacter(input);
                        if (this.lastContent == null)
                        {
                            this.lastContent = EmptyLastStompContentSubFrame.Default;
                        }

                        output.Add(this.lastContent);
                        this.ResetDecoder();
                        break;
                }
            }
            catch (Exception e)
            {
                IStompContentSubFrame errorContent = new DefaultLastStompContentSubFrame(Unpooled.Empty);
                errorContent.Result = DecoderResult.Failure(e);
                output.Add(errorContent);
                this.Checkpoint(ParseState.BadFrame);
            }
        }

        static void SkipNullCharacter(IByteBuffer buffer)
        {
            byte b = buffer.ReadByte();
            if (b != StompConstants.Null)
            {
                throw new InvalidStateException("unexpected byte in buffer " + b + " while expecting NULL byte");
            }
        }

        void ResetDecoder()
        {
            this.Checkpoint(ParseState.SkipControlCharacters);
            this.contentLength = -1;
            this.alreadyReadChunkSize = 0;
            this.lastContent = null;
        }
    }
}