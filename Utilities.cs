using System;
using System.Threading;

namespace ResxPropertiesBuilder
{
    internal sealed class ProjectUtilities
    {
        public static bool IsCriticalException(Exception ex)
        {
            switch (ex)
            {
                case NullReferenceException _:
                case StackOverflowException _:
                case OutOfMemoryException _:
                    return true;
                default:
                    return ex is ThreadAbortException;
            }
        }
    }
}
