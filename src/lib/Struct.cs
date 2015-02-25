using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Struct : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_size_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_fields_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_functions_get(IntPtr i);

		internal Struct()
		{
		}

		internal Struct(IntPtr p) : this(p, true)
		{
		}

		public Struct(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public long Size
		{
			get {
				IntPtr i = ender_item_struct_size_get(raw);
				unsafe
				{
					if (sizeof(IntPtr) == 4)
						return i.ToInt32();
					else
						return i.ToInt64();
				}
			}
		}

		public List Fields
		{
			get {
				IntPtr l = ender_item_struct_fields_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Attr), true, true);
				return list;
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_struct_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}
	}
}
