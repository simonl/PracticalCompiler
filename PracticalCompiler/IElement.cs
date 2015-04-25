namespace PracticalCompiler
{
    public interface IElement<out T>
    {
        uint Index { get; }
        T Content { get; }
    }

    public sealed class Element<T> : IElement<T>
    {
        public uint Index { get; private set; }
        public T Content { get; private set; }

        public Element(uint index, T content)
        {
            Index = index;
            Content = content;
        }
    }
}