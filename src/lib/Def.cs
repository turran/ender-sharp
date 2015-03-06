using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Def : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_def_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_def_functions_get(IntPtr i);
		internal Def()
		{
		}

		internal Def(IntPtr p) : this(p, true)
		{
		}

		public Def(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item DefType {
			get {
				IntPtr t = ender_item_def_type_get(raw);
				return Item.Create(t);
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_def_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

	}
}

