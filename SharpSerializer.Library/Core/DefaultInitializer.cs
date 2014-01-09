using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml;
using Serialization.Advanced;
using Serialization.Advanced.Serializing;
using Serialization.Advanced.Xml;


namespace Serialization.Core
{
    /// <summary>
    ///   Gives standard settings for the framework. Is used only internally.
    /// </summary>
    internal static class DefaultInitializer
    {
        public static XmlWriterSettings GetXmlWriterSettings()
        {
            return GetXmlWriterSettings(Encoding.UTF8);
        }


        public static XmlWriterSettings GetXmlWriterSettings(Encoding encoding)
        {
            var settings = new XmlWriterSettings {Encoding = encoding, Indent = true, OmitXmlDeclaration = true};
	        return settings;
        }

        public static XmlReaderSettings GetXmlReaderSettings()
        {
            var settings = new XmlReaderSettings {IgnoreComments = true, IgnoreWhitespace = true};
	        return settings;
        }

        public static ITypeNameConverter GetTypeNameConverter(bool includeAssemblyVersion, bool includeCulture,
                                                              bool includePublicKeyToken)
        {
            return new TypeNameConverter(includeAssemblyVersion, includeCulture, includePublicKeyToken);
        }

        public static ISimpleValueConverter GetSimpleValueConverter(CultureInfo cultureInfo, ITypeNameConverter typeNameConverter)
        {
            return new SimpleValueConverter(cultureInfo, typeNameConverter);
        }
    }
}