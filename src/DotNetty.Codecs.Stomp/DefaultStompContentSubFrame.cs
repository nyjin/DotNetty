using DotNetty.Buffers;

namespace DotNetty.Codecs.Stomp
{
    public class DefaultStompContentSubframe : DefaultByteBufferHolder, IStompContentSubFrame
    {
        public DefaultStompContentSubframe(IByteBuffer content) : base(content)
        {
            this.Result = DecoderResult.Success;
        }

        public override IByteBufferHolder Replace(IByteBuffer content)
        {
            return new DefaultStompContentSubframe(content);
        }

        public override string ToString()
        {
            return "DefaultStompContent{" +
                "decoderResult=" + this.Result +
                '}';
        }

        public DecoderResult Result { get; set; }
    }
}
