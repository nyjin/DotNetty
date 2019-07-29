// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using DotNetty.Common.Utilities;

    public static class StompHeaderNames
    {
        public static readonly AsciiString AcceptVersion = AsciiString.Cached("accept-version");
        public static readonly AsciiString Host = AsciiString.Cached("host");
        public static readonly AsciiString Login = AsciiString.Cached("login");
        public static readonly AsciiString PassCode = AsciiString.Cached("passcode");
        public static readonly AsciiString HeartBeat = AsciiString.Cached("heart-beat");
        public static readonly AsciiString Version = AsciiString.Cached("version");
        public static readonly AsciiString Session = AsciiString.Cached("session");
        public static readonly AsciiString Server = AsciiString.Cached("server");
        public static readonly AsciiString Destination = AsciiString.Cached("destination");
        public static readonly AsciiString Id = AsciiString.Cached("id");
        public static readonly AsciiString Ack = AsciiString.Cached("ack");
        public static readonly AsciiString Transaction = AsciiString.Cached("transaction");
        public static readonly AsciiString Receipt = AsciiString.Cached("receipt");
        public static readonly AsciiString MessageId = AsciiString.Cached("message-id");
        public static readonly AsciiString Subscription = AsciiString.Cached("subscription");
        public static readonly AsciiString ReceiptId = AsciiString.Cached("receipt-id");
        public static readonly AsciiString Message = AsciiString.Cached("message");
        public static readonly AsciiString ContentLength = AsciiString.Cached("content-length");
        public static readonly AsciiString ContentType = AsciiString.Cached("content-type");
    }
}