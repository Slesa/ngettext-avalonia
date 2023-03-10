using System;
using System.Globalization;
using System.Linq;

namespace Xunit.Sdk
{
    /// <summary>
    /// Exception thrown when Assert.Collection fails.
    /// </summary>
#if XUNIT_VISIBILITY_INTERNAL 
    internal
#else
    public
#endif
    class CollectionException : XunitException
    {
        readonly string innerException;
        readonly string innerStackTrace;

        /// <summary>
        /// Creates a new instance of the <see cref="CollectionException"/> class.
        /// </summary>
        /// <param name="collection">The collection that failed the test.</param>
        /// <param name="expectedCount">The expected number of items in the collection.</param>
        /// <param name="actualCount">The actual number of items in the collection.</param>
        /// <param name="indexFailurePoint">The index of the position where the first comparison failure occurred.</param>
        /// <param name="innerException">The exception that was thrown during the comparison failure.</param>
        public CollectionException(object collection, int expectedCount, int actualCount, int indexFailurePoint = -1, Exception innerException = null)
            : base("Assert.Collection() Failure")
        {
            Collection = collection;
            ExpectedCount = expectedCount;
            ActualCount = actualCount;
            IndexFailurePoint = indexFailurePoint;
            this.innerException = FormatInnerException(innerException);
            innerStackTrace = innerException == null ? null : innerException.StackTrace;
        }

        /// <summary>
        /// The collection that failed the test.
        /// </summary>
        public object Collection { get; set; }

        /// <summary>
        /// The actual number of items in the collection.
        /// </summary>
        public int ActualCount { get; set; }

        /// <summary>
        /// The expected number of items in the collection.
        /// </summary>
        public int ExpectedCount { get; set; }

        /// <summary>
        /// The index of the position where the first comparison failure occurred, or -1 if
        /// comparisions did not occur (because the actual and expected counts differed).
        /// </summary>
        public int IndexFailurePoint { get; set; }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                if (IndexFailurePoint >= 0)
                    return string.Format(CultureInfo.CurrentCulture,
                                         "{0}{4}Collection: {1}{4}Error during comparison of item at index {2}{4}Inner exception: {3}",
                                         base.Message,
                                         ArgumentFormatter.Format(Collection),
                                         IndexFailurePoint,
                                         innerException,
                                         Environment.NewLine);

                return string.Format(CultureInfo.CurrentCulture,
                                     "{0}{4}Collection: {1}{4}Expected item count: {2}{4}Actual item count:   {3}",
                                     base.Message,
                                     ArgumentFormatter.Format(Collection),
                                     ExpectedCount,
                                     ActualCount,
                                     Environment.NewLine);
            }
        }

        /// <inheritdoc/>
        public override string StackTrace
        {
            get
            {
                if (innerStackTrace == null)
                    return base.StackTrace;

                return innerStackTrace + Environment.NewLine + base.StackTrace;
            }
        }

        static string FormatInnerException(Exception innerException)
        {
            if (innerException == null)
                return null;

            var lines = innerException.Message
                                      .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                      .Select((value, idx) => idx > 0 ? "        " + value : value);

            return string.Join(Environment.NewLine, lines);
        }
    }
}
