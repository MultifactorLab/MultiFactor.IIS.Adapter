using System;

namespace MultiFactor.IIS.Adapter.Exceptions
{
    [Serializable]
    internal class MultifactorApiUnreachableException : Exception
    {
        public MultifactorApiUnreachableException() { }
        public MultifactorApiUnreachableException(string message) : base(message) { }
        public MultifactorApiUnreachableException(string message, Exception inner) : base(message, inner) { }
        protected MultifactorApiUnreachableException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}