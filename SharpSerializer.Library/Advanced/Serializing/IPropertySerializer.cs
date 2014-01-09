using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Serialization.Core;


namespace Serialization.Advanced.Serializing
{
    /// <summary>
    ///   Serializes property to a stream
    /// </summary>
    public interface IPropertySerializer
    {
        /// <summary>
        ///   Open the stream for writing
        /// </summary>
        /// <param name = "stream"></param>
        void Open(Stream stream);

        /// <summary>
        ///   Serializes property
        /// </summary>
        /// <param name = "property"></param>
        void Serialize(Property property);

        /// <summary>
        ///   Cleaning, but the stream can be used further
        /// </summary>
        void Close();
    }
}