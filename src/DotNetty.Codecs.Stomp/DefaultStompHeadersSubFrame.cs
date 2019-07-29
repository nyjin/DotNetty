// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    public class DefaultStompHeadersSubFrame : IStompHeadersSubFrame
    {
        public DefaultStompHeadersSubFrame(StompCommand command)
            : this(command, null)
        {
            
        }
        public DefaultStompHeadersSubFrame(StompCommand command, DefaultStompHeaders headers)
        {
            this.Command = command;
            this.Headers = headers;
            this.Result = DecoderResult.Success;
        }

        public DecoderResult Result { get; set; }

        public StompCommand Command { get; }

        public IStompHeaders Headers { get; }
    }
}