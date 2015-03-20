using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Constant : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_constant_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_constant_value_get(IntPtr i, out IntPtr v);

		internal Constant()
		{
		}

		internal Constant(IntPtr p) : this(p, true)
		{
		}

		public Constant(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item ConstantType
		{
			get {
				IntPtr t = ender_item_constant_type_get(raw);
				return Item.Create(t);
			}
		}
	}
}

