using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;


namespace Serialization
{
    /// <summary>
    ///   All labeled with that Attribute object properties are ignored during the serialization. See PropertyProvider
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class ExcludeFromSerializationAttribute : Attribute
    {
    }
}