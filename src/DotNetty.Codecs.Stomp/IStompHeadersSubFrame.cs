// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    public interface IStompHeadersSubFrame : IStompSubFrame
    {
        ///
        StompCommand Command { get; }

        /**
         * Returns headers of this frame.
         */
        IStompHeaders Headers { get; }
    }
}