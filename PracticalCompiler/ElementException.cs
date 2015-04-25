using System;

namespace PracticalCompiler
{
    public sealed class ElementException : Exception
    {
        public readonly uint Index;

        public ElementException(uint index, Exception error) : base("Error while processing " + index + "th element", error)
        {
            Index = index;
        }
    }
}