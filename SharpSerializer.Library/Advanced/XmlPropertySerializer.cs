using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Serialization.Advanced.Serializing;
using Serialization.Advanced.Xml;
using Serialization.Core;
using Serialization.Core.Xml;
using Serialization.Serializing;


namespace Serialization.Advanced
{
    /// <summary>
    ///   Serializes properties to xml or any other target which supports node/attribute notation
    /// </summary>
    /// <remarks>
    ///   Use an instance of your own IXmlWriter in the constructor to target other storage standards
    /// </remarks>
    public sealed class XmlPropertySerializer : PropertySerializer
    {
        private readonly IXmlWriter _writer;

        ///<summary>
        ///</summary>
        ///<param name = "writer"></param>
        public XmlPropertySerializer(IXmlWriter writer)
        {
	        Contract.Requires<ArgumentNullException>(writer != null, "writer");
	        _writer = writer;
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeNullProperty(PropertyTypeInfo<NullProperty> property)
        {
            // nulls must be serialized also 
            _writeStartProperty(Elements.Null, property.Name, property.ValueType);
            _writeEndProperty();
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeSimpleProperty(PropertyTypeInfo<SimpleProperty> property)
        {
            if (property.Property.Value == null) return;

            _writeStartProperty(Elements.SimpleObject, property.Name, property.ValueType);

            _writer.WriteAttribute(Attributes.Value, property.Property.Value);

            _writeEndProperty();
        }

        private void _writeEndProperty()
        {
            _writer.WriteEndElement();
        }

        private void _writeStartProperty(string elementId, string propertyName, Type propertyType)
        {
            _writer.WriteStartElement(elementId);

            // Name
            if (!string.IsNullOrEmpty(propertyName))
            {
                _writer.WriteAttribute(Attributes.Name, propertyName);
            }

            // Type
            if (propertyType != null)
            {
                _writer.WriteAttribute(Attributes.Type, propertyType);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeMultiDimensionalArrayProperty(
            PropertyTypeInfo<MultiDimensionalArrayProperty> property)
        {
            _writeStartProperty(Elements.MultiArray, property.Name, property.ValueType);

            // additional attribute with referenceId
            if (property.Property.Reference.Count > 1)
            {
                _writer.WriteAttribute(Attributes.ReferenceId, property.Property.Reference.Id);
            }

            // DimensionInfos
            _writeDimensionInfos(property.Property.DimensionInfos);

            // Einträge
            _writeMultiDimensionalArrayItems(property.Property.Items, property.Property.ElementType);

            _writeEndProperty();
        }

        private void _writeMultiDimensionalArrayItems(IEnumerable<MultiDimensionalArrayItem> items, Type defaultItemType)
        {
            _writer.WriteStartElement(SubElements.Items);
            foreach (MultiDimensionalArrayItem item in items)
            {
                _writeMultiDimensionalArrayItem(item, defaultItemType);
            }
            _writer.WriteEndElement();
        }

        private void _writeMultiDimensionalArrayItem(MultiDimensionalArrayItem item, Type defaultTypeOfItemValue)
        {
            _writer.WriteStartElement(SubElements.Item);

            // Write Indexes
            _writer.WriteAttribute(Attributes.Indexes, item.Indexes);

            // Write Data
            SerializeCore(new PropertyTypeInfo<Property>(item.Value, defaultTypeOfItemValue));

            _writer.WriteEndElement();
        }


        private void _writeDimensionInfos(IEnumerable<DimensionInfo> infos)
        {
            _writer.WriteStartElement(SubElements.Dimensions);
            foreach (DimensionInfo info in infos)
            {
                _writeDimensionInfo(info);
            }
            _writer.WriteEndElement();
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeSingleDimensionalArrayProperty(
            PropertyTypeInfo<SingleDimensionalArrayProperty> property)
        {
            _writeStartProperty(Elements.SingleArray, property.Name, property.ValueType);

            // additional attribute with referenceId
            if (property.Property.Reference.Count > 1)
            {
                _writer.WriteAttribute(Attributes.ReferenceId, property.Property.Reference.Id);
            }

            // LowerBound
            if (property.Property.LowerBound != 0)
            {
                _writer.WriteAttribute(Attributes.LowerBound, property.Property.LowerBound);
            }

            // items
            _writeItems(property.Property.Items, property.Property.ElementType);

            _writeEndProperty();
        }

        private void _writeDimensionInfo(DimensionInfo info)
        {
            _writer.WriteStartElement(SubElements.Dimension);
            if (info.Length != 0)
            {
                _writer.WriteAttribute(Attributes.Length, info.Length);
            }
            if (info.LowerBound != 0)
            {
                _writer.WriteAttribute(Attributes.LowerBound, info.LowerBound);
            }

            _writer.WriteEndElement();
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeDictionaryProperty(PropertyTypeInfo<DictionaryProperty> property)
        {
            _writeStartProperty(Elements.Dictionary, property.Name, property.ValueType);

            // additional attribute with referenceId
            if (property.Property.Reference.Count > 1)
            {
                _writer.WriteAttribute(Attributes.ReferenceId, property.Property.Reference.Id);
            }

            // Properties
            _writeProperties(property.Property.Properties, property.Property.Type);

            // Items
            _writeDictionaryItems(property.Property.Items, property.Property.KeyType, property.Property.ValueType);

            _writeEndProperty();
        }

        private void _writeDictionaryItems(IEnumerable<KeyValueItem> items, Type defaultKeyType, Type defaultValueType)
        {
            _writer.WriteStartElement(SubElements.Items);
            foreach (KeyValueItem item in items)
            {
                _writeDictionaryItem(item, defaultKeyType, defaultValueType);
            }
            _writer.WriteEndElement();
        }

        private void _writeDictionaryItem(KeyValueItem item, Type defaultKeyType, Type defaultValueType)
        {
            _writer.WriteStartElement(SubElements.Item);
            SerializeCore(new PropertyTypeInfo<Property>(item.Key, defaultKeyType));
            SerializeCore(new PropertyTypeInfo<Property>(item.Value, defaultValueType));
            _writer.WriteEndElement();
        }

        private void _writeValueType(Type type)
        {
            if (type != null)
            {
                _writer.WriteAttribute(Attributes.ValueType, type);
            }
        }

        private void _writeKeyType(Type type)
        {
            if (type != null)
            {
                _writer.WriteAttribute(Attributes.KeyType, type);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeCollectionProperty(PropertyTypeInfo<CollectionProperty> property)
        {
            _writeStartProperty(Elements.Collection, property.Name, property.ValueType);

            // additional attribute with referenceId
            if (property.Property.Reference.Count > 1)
            {
                _writer.WriteAttribute(Attributes.ReferenceId, property.Property.Reference.Id);
            }

            // Properties
            _writeProperties(property.Property.Properties, property.Property.Type);

            //Items
            _writeItems(property.Property.Items, property.Property.ElementType);

            _writeEndProperty();
        }

        private void _writeItems(IEnumerable<Property> properties, Type defaultItemType)
        {
            _writer.WriteStartElement(SubElements.Items);
            foreach (Property item in properties)
            {
                SerializeCore(new PropertyTypeInfo<Property>(item, defaultItemType));
            }
            _writer.WriteEndElement();
        }

        /// <summary>
        ///   Properties are only saved if at least one property exists
        /// </summary>
        /// <param name = "properties"></param>
        /// <param name = "ownerType">to which type belong the properties</param>
        private void _writeProperties(ICollection<Property> properties, Type ownerType)
        {
            // check if there are properties
            if (properties.Count == 0) return;

            _writer.WriteStartElement(SubElements.Properties);
            foreach (Property property in properties) {
	            PropertyInfo propertyInfo = ownerType.GetProperty(property.Name);
	            SerializeCore(propertyInfo != null
		            ? new PropertyTypeInfo<Property>(property, propertyInfo.PropertyType)
		            : new PropertyTypeInfo<Property>(property, null));
            }
	        _writer.WriteEndElement();
        }

        /// <summary>
        /// </summary>
        /// <param name = "property"></param>
        protected override void SerializeComplexProperty(PropertyTypeInfo<ComplexProperty> property)
        {
            _writeStartProperty(Elements.ComplexObject, property.Name, property.ValueType);

            // additional attribute with referenceId
            if (property.Property.Reference.Count>1)
            {
                _writer.WriteAttribute(Attributes.ReferenceId, property.Property.Reference.Id);
            }

            // Properties
            _writeProperties(property.Property.Properties, property.Property.Type);

            _writeEndProperty();
        }

        /// <summary>
        /// Stores only reference to an object, not the object itself
        /// </summary>
        /// <param name="referenceTarget"></param>
        protected override void SerializeReference(ReferenceTargetProperty referenceTarget)
        {
            _writeStartProperty(Elements.Reference, referenceTarget.Name, null);
            _writer.WriteAttribute(Attributes.ReferenceId, referenceTarget.Reference.Id);
            _writeEndProperty();
        }

        /// <summary>
        ///   Open the writer
        /// </summary>
        /// <param name = "stream"></param>
        public override void Open(Stream stream)
        {
            _writer.Open(stream);
        }

        /// <summary>
        ///   Close the Write, but do not close the stream
        /// </summary>
        public override void Close()
        {
            _writer.Close();
        }
    }
}