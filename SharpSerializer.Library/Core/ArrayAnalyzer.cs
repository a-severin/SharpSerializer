using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;


namespace Serialization.Core
{
    /// <summary>
    ///   Gives information about actually analysed array (from the constructor)
    /// </summary>
    public class ArrayAnalyzer
    {
        private readonly object _array;
        private readonly ArrayInfo _arrayInfo;
        private IList<int[]> _indexes;

        ///<summary>
        ///</summary>
        ///<param name = "array"></param>
        public ArrayAnalyzer(object array)
        {
            _array = array;
            var type = array.GetType();
            _arrayInfo = _getArrayInfo(type);
        }

        /// <summary>
        ///   Contains extended information about the current array
        /// </summary>
        public ArrayInfo ArrayInfo
        {
            get { return _arrayInfo; }
        }

        /// <summary>
        ///   How many dimensions. There can be at least 1
        /// </summary>
        /// <returns></returns>
        private static int _getRank(Type arrayType)
        {
            return arrayType.GetArrayRank();
        }

        /// <summary>
        ///   How many items in one dimension
        /// </summary>
        /// <param name = "dimension">0-based</param>
        /// <returns></returns>
        /// <param name="arrayType"></param>
        private int _getLength(int dimension, Type arrayType)
        {
            MethodInfo methodInfo = arrayType.GetMethod("GetLength");
            var length = (int) methodInfo.Invoke(_array, new object[] {dimension});
            return length;
        }

        /// <summary>
        ///   Lower index of an array. Default is 0.
        /// </summary>
        /// <param name = "dimension">0-based</param>
        /// <returns></returns>
        /// <param name="arrayType"></param>
        private int _getLowerBound(int dimension, Type arrayType)
        {
            return _getBound("GetLowerBound", dimension, arrayType);
        }


//        private int getUpperBound(int dimension)
//        {
        // Not used, as UpperBound is equal LowerBound+Length
//            return getBound("GetUpperBound", dimension);
//        }

        private int _getBound(string methodName, int dimension, Type arrayType)
        {
	        Contract.Requires<ArgumentNullException>(methodName != null, "methodName");
	        MethodInfo methodInfo = arrayType.GetMethod(methodName);
            var bound = (int) methodInfo.Invoke(_array, new object[] {dimension});
            return bound;
        }

        private ArrayInfo _getArrayInfo(Type arrayType)
        {
            // Caching is innacceptable, as an array of type string can have different bounds

            var info = new ArrayInfo();

            // Fill the dimension infos
            for (int dimension = 0; dimension < _getRank(arrayType); dimension++)
            {
                var dimensionInfo = new DimensionInfo {
	                Length = _getLength(dimension, arrayType),
	                LowerBound = _getLowerBound(dimension, arrayType)
                };
	            info.DimensionInfos.Add(dimensionInfo);
            }


            return info;
        }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public IEnumerable<int[]> GetIndexes()
        {
            if (_indexes == null)
            {
                _indexes = new List<int[]>();
                ForEach(_addIndexes);
            }

            foreach (var item in _indexes)
            {
                yield return item;
            }
        }

        ///<summary>
        ///</summary>
        ///<returns></returns>
        public IEnumerable<object> GetValues()
        {
            foreach (var indexSet in GetIndexes())
            {
                object value = ((Array) _array).GetValue(indexSet);
                yield return value;
            }
        }

        private void _addIndexes(int[] obj)
        {
            _indexes.Add(obj);
        }


        ///<summary>
        ///</summary>
        ///<param name = "action"></param>
        public void ForEach(Action<int[]> action)
        {
            DimensionInfo dimensionInfo = _arrayInfo.DimensionInfos[0];
            for (int index = dimensionInfo.LowerBound; index < dimensionInfo.LowerBound + dimensionInfo.Length; index++)
            {
                var result = new List<int> {index};

                // Adding the first coordinate

	            if (_arrayInfo.DimensionInfos.Count < 2)
                {
                    // only one dimension
                    action.Invoke(result.ToArray());
                    continue;
                }

                // further dimensions
                _forEach(_arrayInfo.DimensionInfos, 1, result, action);
            }
        }


        /// <summary>
        ///   This functiona will be recursively used
        /// </summary>
        /// <param name = "dimensionInfos"></param>
        /// <param name = "dimension"></param>
        /// <param name = "coordinates"></param>
        /// <param name = "action"></param>
        private void _forEach(IList<DimensionInfo> dimensionInfos, int dimension, IEnumerable<int> coordinates,
                             Action<int[]> action)
        {
			Contract.Requires(dimension >= 0);
            DimensionInfo dimensionInfo = dimensionInfos[dimension];
            for (int index = dimensionInfo.LowerBound; index < dimensionInfo.LowerBound + dimensionInfo.Length; index++)
            {
                var result = new List<int>(coordinates) {index};

                // Adding the first coordinate

	            if (dimension == _arrayInfo.DimensionInfos.Count - 1)
                {
                    // This is the last dimension
                    action.Invoke(result.ToArray());
                    continue;
                }

                // Further dimensions
                _forEach(_arrayInfo.DimensionInfos, dimension + 1, result, action);
            }
        }
    }

    /// <summary>
    ///   Contain info about array (i.e. how many dimensions, lower/upper bounds)
    /// </summary>
    public sealed class ArrayInfo
    {
        private IList<DimensionInfo> _dimensionInfos;

        ///<summary>
        ///</summary>
        public IList<DimensionInfo> DimensionInfos
        {
            get {
	            return _dimensionInfos ?? (_dimensionInfos = new List<DimensionInfo>());
            }
	        set { _dimensionInfos = value; }
        }
    }
}