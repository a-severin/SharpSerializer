using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Serialization.Advanced;
using Serialization.Advanced.Deserializing;
using Serialization.Advanced.Serializing;
using Serialization.Advanced.Xml;
using Serialization.Core;
using Serialization.Deserializing;
using Serialization.Serializing;


namespace Serialization {
	/// <summary>
	///     This is the main class of SharpSerializer. It serializes and deserializes objects.
	/// </summary>
	public sealed class SharpSerializer {
		private IPropertyDeserializer _deserializer;
		private PropertyProvider _propertyProvider;
		private string _rootName;
		private IPropertySerializer _serializer;

		/// <summary>
		///     Standard Constructor. Default is Xml serializing
		/// </summary>
		public SharpSerializer() {
			_initialize(new SharpSerializerXmlSettings());
		}

		/// <summary>
		///     Xml serialization with custom settings
		/// </summary>
		/// <param name="settings"></param>
		public SharpSerializer(SharpSerializerXmlSettings settings) {
			Contract.Requires<ArgumentNullException>(settings != null);
			_initialize(settings);
		}

		/// <summary>
		///     Custom serializer and deserializer
		/// </summary>
		/// <param name="serializer"></param>
		/// <param name="deserializer"></param>
		public SharpSerializer(IPropertySerializer serializer, IPropertyDeserializer deserializer) {
			Contract.Requires<ArgumentNullException>(serializer != null);
			Contract.Requires<ArgumentNullException>(deserializer != null);

			_serializer = serializer;
			_deserializer = deserializer;
		}

		/// <summary>
		///     Default it is an instance of PropertyProvider. It provides all properties to serialize.
		///     You can use an Inheritor and overwrite its GetAllProperties and IgnoreProperty methods to implement your custom
		///     rules.
		/// </summary>
		public PropertyProvider PropertyProvider {
			get {
				return _propertyProvider ?? (_propertyProvider = new PropertyProvider());
			}
			set {
				_propertyProvider = value;
			}
		}

		/// <summary>
		///     What name should have the root property. Default is "Root".
		/// </summary>
		public string RootName {
			get {
				return _rootName ?? (_rootName = "Root");
			}
			set {
				_rootName = value;
			}
		}

		private void _initialize(SharpSerializerXmlSettings settings) {
			// PropertiesToIgnore
			PropertyProvider.PropertiesToIgnore = settings.AdvancedSettings.PropertiesToIgnore;
			PropertyProvider.AttributesToIgnore = settings.AdvancedSettings.AttributesToIgnore;
			//RootName
			RootName = settings.AdvancedSettings.RootName;
			// TypeNameConverter)
			ITypeNameConverter typeNameConverter = settings.AdvancedSettings.TypeNameConverter ??
			                                       DefaultInitializer.GetTypeNameConverter(
				                                       settings.IncludeAssemblyVersionInTypeName, settings.IncludeCultureInTypeName,
				                                       settings.IncludePublicKeyTokenInTypeName);
			// SimpleValueConverter
			ISimpleValueConverter simpleValueConverter = settings.AdvancedSettings.SimpleValueConverter ??
			                                             DefaultInitializer.GetSimpleValueConverter(settings.Culture,
				                                             typeNameConverter);
			// XmlWriterSettings
			XmlWriterSettings xmlWriterSettings = DefaultInitializer.GetXmlWriterSettings(settings.Encoding);
			// XmlReaderSettings
			XmlReaderSettings xmlReaderSettings = DefaultInitializer.GetXmlReaderSettings();

			// Create Serializer and Deserializer
			var reader = new DefaultXmlReader(typeNameConverter, simpleValueConverter, xmlReaderSettings);
			var writer = new DefaultXmlWriter(typeNameConverter, simpleValueConverter, xmlWriterSettings);

			_serializer = new XmlPropertySerializer(writer);
			_deserializer = new XmlPropertyDeserializer(reader);
		}


		/// <summary>
		///     Serializing to a file. File will be always new created and closed after the serialization.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="filename"></param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Serialize(object data, string filename) {
			_createDirectoryIfNeccessary(filename);
			using (Stream stream = new FileStream(filename, FileMode.Create, FileAccess.Write)) {
				Serialize(data, stream);
			}
		}

		private static void _createDirectoryIfNeccessary(string filename) {
			string directory = Path.GetDirectoryName(filename);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) {
				Directory.CreateDirectory(directory);
			}
		}

		/// <summary>
		///     Serializing to the stream. After serialization the stream will NOT be closed.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="stream"></param>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Serialize(object data, Stream stream) {
			Contract.Requires<ArgumentNullException>(data != null, "data");

			var factory = new PropertyFactory(PropertyProvider);

			Property property = factory.CreateProperty(RootName, data);

			try {
				_serializer.Open(stream);
				_serializer.Serialize(property);
			} finally {
				_serializer.Close();
			}
		}

		/// <summary>
		///     Deserializing from the file. After deserialization the file will be closed.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public object Deserialize(string filename) {
			using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read)) {
				return Deserialize(stream);
			}
		}

		/// <summary>
		///     Deserialization from the stream. After deserialization the stream will NOT be closed.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public object Deserialize(Stream stream) {
			try {
				// Deserialize Property
				_deserializer.Open(stream);
				Property property = _deserializer.Deserialize();
				_deserializer.Close();

				// create object from Property
				var factory = new ObjectFactory();
				return factory.CreateObject(property);
			} catch (Exception exception) {
				// corrupted Stream
				throw new DeserializingException(
					"An error occured during the deserialization. Details are in the inner exception.", exception);
			}
		}
	}
}