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

		internal Object()
		{
		}

		internal Object(IntPtr p) : this(p, true)
		{
		}

		public Object(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item Inherit
		{
			get {
				IntPtr inherit = ender_item_object_inherit_get(raw);
				return Item.Create(inherit);
			}
		}
		public List Ctors
		{
			get {
				IntPtr l = ender_item_object_ctor_get(raw);
				// TODO return the correct type
				List list = new List(l, typeof(Item), true, true);
				return list;
			}
		}
	}
}
