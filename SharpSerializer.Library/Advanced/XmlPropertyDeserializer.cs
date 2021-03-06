using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using Serialization.Advanced.Deserializing;
using Serialization.Advanced.Xml;
using Serialization.Core;
using Serialization.Core.Xml;
using Serialization.Serializing;


namespace Serialization.Advanced
{
    /// <summary>
    ///   Contains logic to read data stored with XmlPropertySerializer
    /// </summary>
    public sealed class XmlPropertyDeserializer : IPropertyDeserializer
    {
        private readonly IXmlReader _reader;

        /// <summary>
        /// All reference targets already processed. Used to for reference resolution.
        /// </summary>
        private readonly Dictionary<int, ReferenceTargetProperty> _propertyCache =
            new Dictionary<int, ReferenceTargetProperty>();

        ///<summary>
        ///</summary>
        ///<param name = "reader"></param>
        public XmlPropertyDeserializer(IXmlReader reader)
        {
            _reader = reader;
        }

        #region IPropertyDeserializer Members

        /// <summary>
        ///   Open the stream to read
        /// </summary>
        /// <param name = "stream"></param>
        public void Open(Stream stream)
        {
            _reader.Open(stream);
        }

        /// <summary>
        ///   Reading the property
        /// </summary>
        /// <returns></returns>
        public Property Deserialize()
        {
            // give the first valid tag back
            string elementName = _reader.ReadElement();

            // In what xml tag is the property saved
            PropertyArt propertyArt = _getPropertyArtFromString(elementName);

            // check if the property was found
            if (propertyArt == PropertyArt.Unknown) return null;

            Property result = _deserialize(propertyArt, null);
            return result;
        }

        /// <summary>
        ///   Cleans all
        /// </summary>
        public void Close()
        {
            _reader.Close();
        }

        #endregion

        private Property _deserialize(PropertyArt propertyArt, Type expectedType)
        {
            // Establish the property name
            string propertyName = _reader.GetAttributeAsString(Attributes.Name);

            // Establish the property type
			// id propertyType is not defined, we'll take the expectedType)
            Type propertyType = _reader.GetAttributeAsType(Attributes.Type) ?? expectedType;
            
	        // create the property from the tag
            Property property = Property.CreateInstance(propertyArt, propertyName, propertyType);

            // Null property?
            var nullProperty = property as NullProperty;
            if (nullProperty != null)
            {
                return nullProperty;
            }

            // is it simple property?
            var simpleProperty = property as SimpleProperty;
            if (simpleProperty != null)
            {
                _parseSimpleProperty(_reader, simpleProperty);
                return simpleProperty;
            }

            // This is not a null property and not a simple property
            // it could be only ReferenceProperty or a reference

            int referenceId = _reader.GetAttributeAsInt(Attributes.ReferenceId);

            // Adding property to cache, it must be done before deserializing the object.
            // Otherwise stack overflow occures if the object references itself
            var referenceTarget = property as ReferenceTargetProperty;
            if (referenceTarget != null && referenceId > 0)
            {
                referenceTarget.Reference = new ReferenceInfo() {Id = referenceId, IsProcessed = true};
                _propertyCache.Add(referenceId, referenceTarget);
            }

            if (property==null)
            {
                // Property was not created yet, it can be created as a reference from its id
                if (referenceId < 1)
                    // there is no reference, so the property cannot be restored
                    return null;

                property = _createProperty(referenceId, propertyName, propertyType);
                if (property == null)
                    // Reference was not created
                    return null;

                // property was successfully restored as a reference
                return property;
            }

            var multiDimensionalArrayProperty = property as MultiDimensionalArrayProperty;
            if (multiDimensionalArrayProperty != null)
            {
                _parseMultiDimensionalArrayProperty(multiDimensionalArrayProperty);
                return multiDimensionalArrayProperty;
            }

            var singleDimensionalArrayProperty = property as SingleDimensionalArrayProperty;
            if (singleDimensionalArrayProperty != null)
            {
                _parseSingleDimensionalArrayProperty(singleDimensionalArrayProperty);
                return singleDimensionalArrayProperty;
            }

            var dictionaryProperty = property as DictionaryProperty;
            if (dictionaryProperty != null)
            {
                _parseDictionaryProperty(dictionaryProperty);
                return dictionaryProperty;
            }

            var collectionProperty = property as CollectionProperty;
            if (collectionProperty != null)
            {
                _parseCollectionProperty(collectionProperty);
                return collectionProperty;
            }

            var complexProperty = property as ComplexProperty;
            if (complexProperty != null)
            {
                _parseComplexProperty(complexProperty);
                return complexProperty;
            }

            return property;
        }

        private void _parseCollectionProperty(CollectionProperty property)
        {
            // ElementType
            property.ElementType = property.Type != null ? TypeInfo.GetTypeInfo(property.Type).ElementType : null;

            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Properties)
                {
                    // Properties
                    _readProperties(property.Properties, property.Type);
                    continue;
                }

                if (subElement == SubElements.Items)
                {
                    // Items
                    _readItems(property.Items, property.ElementType);
                }
            }
        }

        private void _parseDictionaryProperty(DictionaryProperty property)
        {
            if (property.Type!=null)
            {
                var typeInfo = TypeInfo.GetTypeInfo(property.Type);
                property.KeyType = typeInfo.KeyType;
                property.ValueType = typeInfo.ElementType;
            }

            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Properties)
                {
                    // Properties
                    _readProperties(property.Properties, property.Type);
                    continue;
                }
                if (subElement == SubElements.Items)
                {
                    // Items
                    _readDictionaryItems(property.Items, property.KeyType, property.ValueType);
                }
            }
        }

        private void _readDictionaryItems(IList<KeyValueItem> items, Type expectedKeyType, Type expectedValueType)
        {
            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Item)
                {
                    _readDictionaryItem(items, expectedKeyType, expectedValueType);
                }
            }
        }

        private void _readDictionaryItem(IList<KeyValueItem> items, Type expectedKeyType, Type expectedValueType)
        {
            Property keyProperty = null;
            Property valueProperty = null;
            foreach (string subElement in _reader.ReadSubElements())
            {
                // check if key and value was found
                if (keyProperty != null && valueProperty != null) break;

                // check if valid tag was found
                PropertyArt propertyArt = _getPropertyArtFromString(subElement);
                if (propertyArt == PropertyArt.Unknown) continue;

                // items are as pair key-value defined

                // first is always the key
                if (keyProperty == null)
                {
                    // Key was not defined yet (the first item was found)
                    keyProperty = _deserialize(propertyArt, expectedKeyType);
                    continue;
                }

                // key was defined (the second item was found)
                valueProperty = _deserialize(propertyArt, expectedValueType);
            }

            // create the item
            var item = new KeyValueItem(keyProperty, valueProperty);
            items.Add(item);
        }

        private void _parseMultiDimensionalArrayProperty(MultiDimensionalArrayProperty property)
        {
            property.ElementType = property.Type != null ? TypeInfo.GetTypeInfo(property.Type).ElementType : null;

            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Dimensions)
                {
                    // Read dimensions
                    _readDimensionInfos(property.DimensionInfos);
                }

                if (subElement == SubElements.Items)
                {
                    // Read items
                    _readMultiDimensionalArrayItems(property.Items, property.ElementType);
                }
            }
        }

        private void _readMultiDimensionalArrayItems(IList<MultiDimensionalArrayItem> items, Type expectedElementType)
        {
            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Item)
                {
                    _readMultiDimensionalArrayItem(items, expectedElementType);
                }
            }
        }

        private void _readMultiDimensionalArrayItem(IList<MultiDimensionalArrayItem> items, Type expectedElementType)
        {
            int[] indexes = _reader.GetAttributeAsArrayOfInt(Attributes.Indexes);
            foreach (string subElement in _reader.ReadSubElements())
            {
                PropertyArt propertyArt = _getPropertyArtFromString(subElement);
                if (propertyArt == PropertyArt.Unknown) continue;

                Property value = _deserialize(propertyArt, expectedElementType);
                var item = new MultiDimensionalArrayItem(indexes, value);
                items.Add(item);
            }
        }

        private void _readDimensionInfos(IList<DimensionInfo> dimensionInfos)
        {
            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Dimension)
                {
                    _readDimensionInfo(dimensionInfos);
                }
            }
        }

        private void _readDimensionInfo(IList<DimensionInfo> dimensionInfos)
        {
            var info = new DimensionInfo {
	            Length = _reader.GetAttributeAsInt(Attributes.Length),
	            LowerBound = _reader.GetAttributeAsInt(Attributes.LowerBound)
            };
	        dimensionInfos.Add(info);
        }

        private void _parseSingleDimensionalArrayProperty(SingleDimensionalArrayProperty property)
        {
            // ElementType
            property.ElementType = property.Type != null ? TypeInfo.GetTypeInfo(property.Type).ElementType : null;

            // LowerBound
            property.LowerBound = _reader.GetAttributeAsInt(Attributes.LowerBound);

            // Items
            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Items)
                {
                    _readItems(property.Items, property.ElementType);
                }
            }
        }

        private void _readItems(ICollection<Property> items, Type expectedElementType)
        {
            foreach (string subElement in _reader.ReadSubElements())
            {
                PropertyArt propertyArt = _getPropertyArtFromString(subElement);
                if (propertyArt != PropertyArt.Unknown)
                {
                    // Property is found
                    Property subProperty = _deserialize(propertyArt, expectedElementType);
                    items.Add(subProperty);
                }
            }
        }

        private void _parseComplexProperty(ComplexProperty property)
        {

            foreach (string subElement in _reader.ReadSubElements())
            {
                if (subElement == SubElements.Properties)
                {
                    _readProperties(property.Properties, property.Type);
                }
            }
        }

        private void _readProperties(PropertyCollection properties, Type ownerType)
        {
            foreach (string subElement in _reader.ReadSubElements())
            {
                PropertyArt propertyArt = _getPropertyArtFromString(subElement);
                if (propertyArt != PropertyArt.Unknown)
                {
                    // check if the property with the name exists
                    string subPropertyName = _reader.GetAttributeAsString(Attributes.Name);
                    if (string.IsNullOrEmpty(subPropertyName)) continue;

                    // estimating the propertyInfo
                    PropertyInfo subPropertyInfo = ownerType.GetProperty(subPropertyName);
                    if (subPropertyInfo != null)
                    {
                        Property subProperty = _deserialize(propertyArt, subPropertyInfo.PropertyType);
                        properties.Add(subProperty);
                    }
                }
            }
        }

        private void _parseSimpleProperty(IXmlReader reader, SimpleProperty property)
        {
            property.Value = _reader.GetAttributeAsObject(Attributes.Value, property.Type);
        }

        private Property _createProperty(int referenceId, string propertyName, Type propertyType)
        {
            var cachedProperty = _propertyCache[referenceId];
            var property = (ReferenceTargetProperty)Property.CreateInstance(cachedProperty.Art, propertyName, propertyType);
            cachedProperty.Reference.Count++;
            property.MakeFlatCopyFrom(cachedProperty);
            // Reference must be recreated, cause IsProcessed differs for reference and the full property
            property.Reference = new ReferenceInfo() {Id = referenceId};
            return property;
        }

        private static PropertyArt _getPropertyArtFromString(string name)
        {
            if (name == Elements.SimpleObject) return PropertyArt.Simple;
            if (name == Elements.ComplexObject) return PropertyArt.Complex;
            if (name == Elements.Collection) return PropertyArt.Collection;
            if (name == Elements.SingleArray) return PropertyArt.SingleDimensionalArray;
            if (name == Elements.Null) return PropertyArt.Null;
            if (name == Elements.Dictionary) return PropertyArt.Dictionary;
            if (name == Elements.MultiArray) return PropertyArt.MultiDimensionalArray;
            // is used only for backward compatibility
            if (name == Elements.OldReference) return PropertyArt.Reference;
            // is used since the v.2.12
            if (name == Elements.Reference) return PropertyArt.Reference;

            return PropertyArt.Unknown;
        }
    }
}