using System;

namespace AzureFunctionStaticFiles.Tests
{
    /// <summary>
    /// Null logging scope.
    /// </summary>
    class NullScope : IDisposable
    {
        /// <summary>
        /// Singleton instance.
        /// <summary>
        public static NullScope Instance { get; } = new NullScope();

        /// <summary>
        /// Constructor.
        /// <summary>
        private NullScope() {}

        public void Dispose() {}
    }
}
