// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    /// <summary>
    /// Stomp Command
    /// </summary>
    public enum StompCommand
    {
        STOMP,
        CONNECT,
        CONNECTED,
        SEND,
        SUBSCRIBE,
        UNSUBSCRIBE,
        ACK,
        NACK,
        BEGIN,
        DISCONNECT,
        MESSAGE,
        RECEIPT,
        ERROR,
        UNKNOWN
    }
}