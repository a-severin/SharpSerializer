using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serialization.Advanced;


namespace Serialization.Tests {
	[TestClass()]
	public class SharpSerializerTests {
		[TestMethod()]
		public void ConstructorNoArguments_Normal_Pass() {
			var serializer = new SharpSerializer();
			Assert.IsNotNull(serializer);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ConstructorSharpSerializerXmlSettings_NullArgument_ArgumentException() {
			new SharpSerializer(null);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ConstructorIPropertySerializer_NullArgument_ArgumentException() {
			new SharpSerializer(null, new XmlPropertyDeserializer(new DefaultXmlReader(new TypeNameConverter(), new SimpleValueConverter(), new XmlReaderSettings())));
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ConstructorIPropertyDeserializer_NullArgument_ArgumentException() {
			new SharpSerializer(new XmlPropertySerializer(new DefaultXmlWriter(new TypeNameConverter(), new SimpleValueConverter(), new XmlWriterSettings())), null);
		}

	}
}
