using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;


namespace Serialization.Core
{
    /// <summary>
    ///   Occures if the simple value can not be restored from its text representation
    /// </summary>
#if Smartphone
#elif SILVERLIGHT
#else
    [Serializable]
#endif
    public class SimpleValueParsingException : Exception
    {
        ///<summary>
        ///</summary>
        public SimpleValueParsingException()
        {
        }

        ///<summary>
        ///</summary>
        ///<param name = "message"></param>
        public SimpleValueParsingException(string message) : base(message)
        {
        }

        ///<summary>
        ///</summary>
        ///<param name = "message"></param>
        ///<param name = "innerException"></param>
        public SimpleValueParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }

#if Smartphone
#elif SILVERLIGHT
#else
        /// <summary>
        /// </summary>
        /// <param name = "info"></param>
        /// <param name = "context"></param>
        protected SimpleValueParsingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
#endif
    }
}