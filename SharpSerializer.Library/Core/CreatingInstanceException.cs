using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;


namespace Serialization.Core {
	/// <summary>
	///     Occurs if no instance of a type can be created. Maybe the type lacks on a public standard (parameterless)
	///     constructor?
	/// </summary>
	[Serializable]
	public class CreatingInstanceException : Exception {
		/// <summary>
		/// </summary>
		public CreatingInstanceException() {
		}

		/// <summary>
		/// </summary>
		/// <param name="message"></param>
		public CreatingInstanceException(string message) : base(message) {
		}

		/// <summary>
		/// </summary>
		/// <param name="message"></param>
		/// <param name="innerException"></param>
		public CreatingInstanceException(string message, Exception innerException) : base(message, innerException) {
		}

		/// <summary>
		/// </summary>
		/// <param name="info"></param>
		/// <param name="context"></param>
		protected CreatingInstanceException(SerializationInfo info, StreamingContext context) : base(info, context) {
		}
	}
}