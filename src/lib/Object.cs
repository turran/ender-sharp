using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Object : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_inherit_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_ctor_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_functions_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_props_get(IntPtr i);

		internal Object()
		{
		}

		internal Object(IntPtr p) : this(p, true)
		{
		}

		public Object(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Object Inherit
		{
			get {
				IntPtr inherit = ender_item_object_inherit_get(raw);
				if (inherit == IntPtr.Zero)
					return null;
				return new Object(inherit);
			}
		}
		public List Ctors
		{
			get {
				IntPtr l = ender_item_object_ctor_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_object_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		public List Props
		{
			get {
				IntPtr l = ender_item_object_props_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Attr), true, true);
				return list;

			}
		}
	}
}
