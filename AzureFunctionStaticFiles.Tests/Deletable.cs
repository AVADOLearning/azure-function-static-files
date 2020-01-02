using System;

namespace AzureFunctionStaticFiles.Tests
{
    /// <summary>
    /// Deletable object.
    /// </summary>
    /// <remarks>
    /// Calls the object's Delete() method on dispose.
    /// </remarks>
    class Deletable<T> : IDisposable
    {
        /// <summary>
        /// Deletable object.
        /// </summary>
        public T Value { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Deletable(T value)
        {
            Value = value;
        }

        public void Dispose()
        {
            dynamic x = Value;
            x.Delete();
        }
    }
}
