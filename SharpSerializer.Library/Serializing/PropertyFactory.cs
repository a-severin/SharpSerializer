using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Serialization.Advanced;
using Serialization.Core;


namespace Serialization.Serializing
{
    /// <summary>
    ///   Decomposes object to a Property and its Subproperties
    /// </summary>
    public sealed class PropertyFactory
    {
        private readonly object[] _emptyObjectArray = new object[0];
        private readonly PropertyProvider _propertyProvider;

        /// <summary>
        /// Contains reference targets.
        /// </summary>
        private readonly Dictionary<object, ReferenceTargetProperty> _propertyCache =
            new Dictionary<object, ReferenceTargetProperty>();

        /// <summary>
        /// It will be incremented as neccessary
        /// </summary>
        private int _currentReferenceId = 1;

        /// <summary>
        /// </summary>
        /// <param name = "propertyProvider">provides all important properties of the decomposed object</param>
        public PropertyFactory(PropertyProvider propertyProvider)
        {
            _propertyProvider = propertyProvider;
        }

        /// <summary>
        /// </summary>
        /// <param name = "name"></param>
        /// <param name = "value"></param>
        /// <returns>NullProperty if the value is null</returns>
        public Property CreateProperty(string name, object value)
        {
            if (value == null) return new NullProperty(name);

            // If value type is recognized, it will be taken from typeinfo cache
            TypeInfo typeInfo = TypeInfo.GetTypeInfo(value);

            // Is it simple type
            Property property = _createSimpleProperty(name, typeInfo, value);
            if (property != null)
            {
                // It is simple type
                return property;
            }

            // From now it can only be an instance of ReferenceTargetProperty
            ReferenceTargetProperty referenceTarget = _createReferenceTargetInstance(name, typeInfo);

            // Search in Cache
            ReferenceTargetProperty cachedTarget;
            if (_propertyCache.TryGetValue(value, out cachedTarget))
            {
                // Value was already referenced
                // Its reference will be used
                cachedTarget.Reference.Count++;
                referenceTarget.MakeFlatCopyFrom(cachedTarget);
                return referenceTarget;
            }

            // Target was not found in cache
            // it must be created

            // Adding property to cache
            referenceTarget.Reference = new ReferenceInfo {Id = _currentReferenceId++};
	        _propertyCache.Add(value, referenceTarget);

            // Parsing the property
            var handled = _fillSingleDimensionalArrayProperty(referenceTarget as SingleDimensionalArrayProperty, typeInfo, value);
            handled = handled || _fillMultiDimensionalArrayProperty(referenceTarget as MultiDimensionalArrayProperty, typeInfo, value);
            handled = handled || _fillDictionaryProperty(referenceTarget as DictionaryProperty, typeInfo, value);
            handled = handled || _fillCollectionProperty(referenceTarget as CollectionProperty, typeInfo, value);
            handled = handled || _fillComplexProperty(referenceTarget as ComplexProperty, typeInfo, value);

            if (!handled)
                throw new InvalidOperationException(string.Format("Property cannot be filled. Property: {0}",
                                                                  referenceTarget));
           
            return referenceTarget;
        }

        private static ReferenceTargetProperty _createReferenceTargetInstance(string name, TypeInfo typeInfo)
        {
            // Is it array?
            if (typeInfo.IsArray)
            {
                if (typeInfo.ArrayDimensionCount < 2)
                {
                    // 1D-Array
                    return new SingleDimensionalArrayProperty(name, typeInfo.Type);
                }
                // MultiD-Array
                return new MultiDimensionalArrayProperty(name, typeInfo.Type);
            }

            if (typeInfo.IsDictionary)
            {
                return new DictionaryProperty(name, typeInfo.Type);
            }
            if (typeInfo.IsCollection)
            {
                return new CollectionProperty(name, typeInfo.Type);
            }
            if (typeInfo.IsEnumerable)
            {
                // Actually it would be enough to check if the typeinfo.IsEnumerable is true...
                return new CollectionProperty(name, typeInfo.Type);
            }

            // If nothing was recognized, a complex type will be created
            return new ComplexProperty(name, typeInfo.Type);
        }

        private bool _fillComplexProperty(ComplexProperty property, TypeInfo typeInfo, object value)
        {
            if (property == null)
                return false;

            // Parsing properties
            _parseProperties(property, typeInfo, value);

            return true;
        }

        private void _parseProperties(ComplexProperty property, TypeInfo typeInfo, object value)
        {
            IList<PropertyInfo> propertyInfos = _propertyProvider.GetProperties(typeInfo);
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                object subValue = propertyInfo.GetValue(value, _emptyObjectArray);

                Property subProperty = CreateProperty(propertyInfo.Name, subValue);

                property.Properties.Add(subProperty);
            }
        }


        private bool _fillCollectionProperty(CollectionProperty property, TypeInfo info, object value)
        {
            if (property == null)
                return false;

            // Parsing properties
            _parseProperties(property, info, value);

            // Parse Items
            _parseCollectionItems(property, info, value);

            return true;
        }

        private void _parseCollectionItems(CollectionProperty property, TypeInfo info, object value)
        {
            property.ElementType = info.ElementType;

            var collection = (ICollection) value;
            foreach (object item in collection)
            {
                Property itemProperty = CreateProperty(null, item);

                property.Items.Add(itemProperty);
            }
        }

        private bool _fillDictionaryProperty(DictionaryProperty property, TypeInfo info, object value)
        {
            if (property == null)
                return false;

            // Properties
            _parseProperties(property, info, value);

            // Items
            _parseDictionaryItems(property, info, value);

            return true;
        }

        private void _parseDictionaryItems(DictionaryProperty property, TypeInfo info, object value)
        {
            property.KeyType = info.KeyType;
            property.ValueType = info.ElementType;

            var dictionary = (IDictionary) value;
            foreach (DictionaryEntry entry in dictionary)
            {
                Property keyProperty = CreateProperty(null, entry.Key);

                Property valueProperty = CreateProperty(null, entry.Value);

                property.Items.Add(new KeyValueItem(keyProperty, valueProperty));
            }
        }

        private bool _fillMultiDimensionalArrayProperty(MultiDimensionalArrayProperty property, TypeInfo info, object value)
        {
            if (property == null)
                return false;
            property.ElementType = info.ElementType;

            var analyzer = new ArrayAnalyzer(value);

            // DimensionInfos
            property.DimensionInfos = analyzer.ArrayInfo.DimensionInfos;

            // Items
            foreach (var indexSet in analyzer.GetIndexes())
            {
                object subValue = ((Array) value).GetValue(indexSet);
                Property itemProperty = CreateProperty(null, subValue);

                property.Items.Add(new MultiDimensionalArrayItem(indexSet, itemProperty));
            }
            return true;
        }

        private bool _fillSingleDimensionalArrayProperty(SingleDimensionalArrayProperty property, TypeInfo info, object value)
        {
            if (property == null)
                return false;

            property.ElementType = info.ElementType;

            var analyzer = new ArrayAnalyzer(value);

            // Dimensionen
            DimensionInfo dimensionInfo = analyzer.ArrayInfo.DimensionInfos[0];
            property.LowerBound = dimensionInfo.LowerBound;

            // Items
            foreach (object item in analyzer.GetValues())
            {
                Property itemProperty = CreateProperty(null, item);

                property.Items.Add(itemProperty);
            }

            return true;
        }

        private static Property _createSimpleProperty(string name, TypeInfo typeInfo, object value)
        {
            if (!typeInfo.IsSimple) return null;
            var result = new SimpleProperty(name, typeInfo.Type) {Value = value};
	        return result;
        }
    }
}