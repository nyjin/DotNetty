// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace DotNetty.Codecs
{
    using System;
    using System.Text;
    using DotNetty.Buffers;
    using DotNetty.Common.Utilities;

    public sealed class AsciiHeadersEncoder
    {
        /**
         * The separator characters to insert between a header name and a header value.
         */
        public enum SeparatorType
        {
            /// <summary>
            /// ':'
            /// </summary>
            Colon,

            /// <summary>
            /// ': '
            /// </summary>
            ColonSpace,
        }

        /**
         * The newline characters to insert between header entries.
         */
        public enum NewlineType
        {
            /// <summary>
            /// '\n'
            /// </summary>
            LF,

            /// <summary>
            /// '\r\n'
            /// </summary>
            CRLF
        }

        readonly IByteBuffer buf;
        readonly SeparatorType separatorType;
        readonly NewlineType newlineType;

        public AsciiHeadersEncoder(IByteBuffer buf)
            : this(buf, SeparatorType.ColonSpace, NewlineType.CRLF)
        {
        }

        public AsciiHeadersEncoder(IByteBuffer buf, SeparatorType separatorType, NewlineType newlineType)
        {
            this.buf = buf ?? throw new NullReferenceException("buf");
            this.separatorType = separatorType;
            this.newlineType = newlineType;
        }

        public void Encode(HeaderEntry<ICharSequence, ICharSequence> entry)
        {
            ICharSequence name = entry.Key;
            ICharSequence value = entry.Value;
            IByteBuffer buf = this.buf;
            int nameLen = name.Count;
            int valueLen = value.Count;
            int entryLen = nameLen + valueLen + 4;
            int offset = buf.WriterIndex;
            buf.EnsureWritable(entryLen);
            WriteAscii(buf, offset, name);
            offset += nameLen;

            switch (this.separatorType)
            {
                case SeparatorType.Colon:
                    buf.SetByte(offset++, ':');
                    break;
                case SeparatorType.ColonSpace:
                    buf.SetByte(offset++, ':');
                    buf.SetByte(offset++, ' ');
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            WriteAscii(buf, offset, value);
            offset += valueLen;

            switch (this.newlineType)
            {
                case NewlineType.LF:
                    buf.SetByte(offset++, '\n');
                    break;
                case NewlineType.CRLF:
                    buf.SetByte(offset++, '\r');
                    buf.SetByte(offset++, '\n');
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            buf.SetWriterIndex(offset);
        }

        static void WriteAscii(IByteBuffer buf, int offset, ICharSequence value)
        {
            if (value is AsciiString)
            {
                ByteBufferUtil.Copy((AsciiString)value, 0, buf, offset, value.Count);
            }
            else
            {
                buf.SetCharSequence(offset, value, Encoding.ASCII);
            }
        }
    }
}