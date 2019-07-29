// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using System;

    public class InvalidStateException : Exception
    {
        public InvalidStateException()
        {
        }

        public InvalidStateException(string message)
            : base(message)
        {
        }

        public InvalidStateException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}