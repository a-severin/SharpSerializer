using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Xml;
using Serialization.Advanced.Serializing;
using Serialization.Advanced.Xml;


namespace Serialization.Advanced
{
    /// <summary>
    ///   Reads data which was stored with DefaultXmlWriter
    /// </summary>
    public sealed class DefaultXmlReader : IXmlReader
    {
        private readonly XmlReaderSettings _settings;
        private readonly ITypeNameConverter _typeNameConverter;
        private readonly ISimpleValueConverter _valueConverter;
        private XmlReader _currentReader;
        private Stack<XmlReader> _readerStack;


        ///<summary>
        ///</summary>
        ///<param name = "typeNameConverter"></param>
        ///<param name = "valueConverter"></param>
        ///<param name = "settings"></param>
        ///<exception cref = "ArgumentNullException"></exception>
        public DefaultXmlReader(ITypeNameConverter typeNameConverter, ISimpleValueConverter valueConverter,
                                XmlReaderSettings settings)
        {
			Contract.Requires<ArgumentNullException>(typeNameConverter != null);
			Contract.Requires<ArgumentNullException>(valueConverter != null);
			Contract.Requires<ArgumentNullException>(settings != null);

            _typeNameConverter = typeNameConverter;
            _valueConverter = valueConverter;
            _settings = settings;
        }

        #region IXmlReader Members

        /// <summary>
        ///   Reads next valid element
        /// </summary>
        /// <returns>null if nothing was found</returns>
        public string ReadElement()
        {
            while (_currentReader.Read())
            {
                if (_currentReader.NodeType != XmlNodeType.Element) continue;

                return _currentReader.Name;
            }
            return null;
        }

        /// <summary>
        ///   Reads all sub elements of the current element
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ReadSubElements()
        {
            // Position the reader on an element
            _currentReader.MoveToElement();

            // create the subReader
            XmlReader subReader = _currentReader.ReadSubtree();

            // positions the new XmlReader on the node that was current before the call to ReadSubtree method
            // http://msdn.microsoft.com/query/dev10.query?appId=Dev10IDEF1&l=EN-US&k=k%28SYSTEM.XML.XMLREADER.READSUBTREE%29;k%28TargetFrameworkMoniker-%22.NETFRAMEWORK%2cVERSION%3dV2.0%22%29;k%28DevLang-CSHARP%29&rd=true
            subReader.Read();

            _pushCurrentReader(subReader);

            try
            {
                // read the first valid element
                string name = ReadElement();

                // read further elements
                while (!string.IsNullOrEmpty(name))
                {
                    yield return name;
                    name = ReadElement();
                }
            }
            finally
            {
                // Close the current reader,
                // it positions the parent reader on the last node of the subReader
                subReader.Close();
                // aktualise the current Reader
                _popCurrentReader();
            }
        }


        /// <summary>
        ///   Reads attribute as string
        /// </summary>
        /// <param name = "attributeName"></param>
        /// <returns>null if nothing was found</returns>
        public string GetAttributeAsString(string attributeName)
        {
            if (!_currentReader.MoveToAttribute(attributeName)) return null;
            return _currentReader.Value;
        }

        /// <summary>
        ///   Reads attribute and converts it to type
        /// </summary>
        /// <param name = "attributeName"></param>
        /// <returns>null if nothing found</returns>
        public Type GetAttributeAsType(string attributeName)
        {
            string typeName = GetAttributeAsString(attributeName);
            return _typeNameConverter.ConvertToType(typeName);
        }

        /// <summary>
        ///   Reads attribute and converts it to integer
        /// </summary>
        /// <param name = "attributeName"></param>
        /// <returns>0 if nothing found</returns>
        public int GetAttributeAsInt(string attributeName)
        {
            if (!_currentReader.MoveToAttribute(attributeName)) return 0;
            return _currentReader.ReadContentAsInt();
        }

        /// <summary>
        ///   Reads attribute and converts it as array of int
        /// </summary>
        /// <param name = "attributeName"></param>
        /// <returns>empty array if nothing found</returns>
        public int[] GetAttributeAsArrayOfInt(string attributeName)
        {
            if (!_currentReader.MoveToAttribute(attributeName)) return null;
            return _getArrayOfIntFromText(_currentReader.Value);
        }

        /// <summary>
        ///   Reads attribute and converts it to object of the expectedType
        /// </summary>
        /// <param name = "attributeName"></param>
        /// <param name = "expectedType"></param>
        /// <returns></returns>
        public object GetAttributeAsObject(string attributeName, Type expectedType)
        {
            string objectAsText = GetAttributeAsString(attributeName);
            return _valueConverter.ConvertFromString(objectAsText, expectedType);
        }

        /// <summary>
        ///   Open the stream
        /// </summary>
        /// <param name = "stream"></param>
        public void Open(Stream stream)
        {
            _readerStack = new Stack<XmlReader>();

            XmlReader reader = XmlReader.Create(stream, _settings);

            // set the main reader
            _pushCurrentReader(reader);
        }

        /// <summary>
        ///   Stream can be further used
        /// </summary>
        public void Close()
        {
            _currentReader.Close();
        }

        #endregion

        /// <summary>
        ///   Remove one reader from stack and reset the current reader
        /// </summary>
        private void _popCurrentReader()
        {
            // Remove one reader from the stack
            if (_readerStack.Count > 0)
            {
                _readerStack.Pop();
            }

            if (_readerStack.Count > 0)
            {
                _currentReader = _readerStack.Peek();
                return;
            }

            _currentReader = null;
        }

        /// <summary>
        ///   Add reader to stack and set it the current reader
        /// </summary>
        /// <param name = "reader"></param>
        private void _pushCurrentReader(XmlReader reader)
        {
            _readerStack.Push(reader);

            _currentReader = reader;
        }

        /// <summary>
        ///   Converts text in form "1,2,3,4,5" to int[] {1,2,3,4,5}
        /// </summary>
        /// <param name = "text"></param>
        /// <returns>null if no items are recognized or the text is null or empty</returns>
        private static int[] _getArrayOfIntFromText(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;

            string[] splittedString = text.Split(new[] {','});
            if (splittedString.Length == 0) return null;

            var result = new List<int>();
            foreach (string s in splittedString)
            {
                int i = int.Parse(s);
                result.Add(i);
            }
            return result.ToArray();
        }
    }
}