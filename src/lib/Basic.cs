using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Basic : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		extern static ValueType ender_item_basic_value_type_get(IntPtr i);

		internal Basic()
		{
		}

		internal Basic(IntPtr p) : this(p, true)
		{
		}

		public Basic(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public ValueType ValueType
		{
			get {
				return ender_item_basic_value_type_get(raw);
			}
		}
	}
}



