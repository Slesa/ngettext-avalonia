using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Xunit.Sdk
{
    /// <summary>
    /// Exception thrown when an All assertion has one or more items fail an assertion.
    /// </summary>
#if XUNIT_VISIBILITY_INTERNAL 
    internal
#else
    public
#endif
    class AllException : XunitException
    {
        readonly IReadOnlyList<Tuple<int, object, Exception>> errors;
        readonly int totalItems;

        /// <summary>
        /// Creates a new instance of the <see cref="AllException"/> class.
        /// </summary>
        /// <param name="totalItems">The total number of items that were in the collection.</param>
        /// <param name="errors">The list of errors that occurred during the test pass.</param>
        public AllException(int totalItems, Tuple<int, object, Exception>[] errors)
            : base("Assert.All() Failure")
        {
            this.errors = errors;
            this.totalItems = totalItems;
        }

        /// <summary>
        /// The errors that occurred during execution of the test.
        /// </summary>
        public IReadOnlyList<Exception> Failures { get { return errors.Select(t => t.Item3).ToList(); } }

        /// <inheritdoc/>
        public override string Message
        {
            get
            {
                var formattedErrors = errors.Select(error =>
                {
                    var indexString = string.Format(CultureInfo.CurrentCulture, "[{0}]: ", error.Item1);
                    var spaces = Environment.NewLine + "".PadRight(indexString.Length);

                    return string.Format(CultureInfo.CurrentCulture,
                                         "{0}Item: {1}{2}{3}",
                                         indexString,
                                         error.Item2.ToString().Replace(Environment.NewLine, spaces),
                                         spaces,
                                         error.Item3.ToString().Replace(Environment.NewLine, spaces));
                });

                return string.Format(CultureInfo.CurrentCulture,
                                     "{0}: {1} out of {2} items in the collection did not pass.{3}{4}",
                                     base.Message,
                                     errors.Count,
                                     totalItems,
                                     Environment.NewLine,
                                     string.Join(Environment.NewLine, formattedErrors));
            }
        }
    }
}