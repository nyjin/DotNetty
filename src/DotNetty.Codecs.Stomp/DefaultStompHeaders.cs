// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace DotNetty.Codecs.Stomp
{
    using DotNetty.Common.Utilities;

    public class DefaultStompHeaders : DefaultHeaders<ICharSequence, ICharSequence>, IStompHeaders
    {
        public DefaultStompHeaders(IValueConverter<ICharSequence> valueConverter)
            : base(valueConverter)
        {
        }

        public DefaultStompHeaders(IValueConverter<ICharSequence> valueConverter, INameValidator<ICharSequence> nameValidator)
            : base(valueConverter, nameValidator)
        {
        }

        public DefaultStompHeaders(IHashingStrategy<ICharSequence> nameHashingStrategy, IValueConverter<ICharSequence> valueConverter, INameValidator<ICharSequence> nameValidator)
            : base(nameHashingStrategy, valueConverter, nameValidator)
        {
        }

        public DefaultStompHeaders(IHashingStrategy<ICharSequence> nameHashingStrategy, IValueConverter<ICharSequence> valueConverter, INameValidator<ICharSequence> nameValidator, int arraySizeHint)
            : base(nameHashingStrategy, valueConverter, nameValidator, arraySizeHint)
        {
        }

        public new bool Contains(ICharSequence name, ICharSequence value)
        {
            return this.Contains(name, value, false);
        }
        public bool Contains(ICharSequence name, ICharSequence value, bool ignoreCase)
        {
            return base.Contains(name, value, ignoreCase ? AsciiString.CaseInsensitiveHasher : AsciiString.CaseSensitiveHasher);
        }
    }
}