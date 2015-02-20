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

		internal Object()
		{
		}

		internal Object(IntPtr p) : base(p)
		{
		}
	}
}
