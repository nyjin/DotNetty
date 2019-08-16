// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Stomp.Client
{
    using System.Collections.Generic;
    using DotNetty.Codecs.Stomp;
    using DotNetty.Transport.Channels;

    public class StompSubFramePublicEncoder : StompSubFrameEncoder
    {
        public new void Encode(IChannelHandlerContext context, IStompSubFrame message, List<object> output)
        {
            base.Encode(context, message, output);
        }
    }
}