using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Enum : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_enum_values_get(IntPtr i);

		internal Enum()
		{
		}

		internal Enum(IntPtr p) : this(p, true)
		{
		}

		public Enum(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public List Values
		{
			get {
				IntPtr l = ender_item_enum_values_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Constant), true, true);
				return list;
			}
		}
	}
}

