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

		#region Item interface
		public override string ManagedType(Generator generator)
		{
			// TODO if the created object is actually an enum (it can be a class in
			// case it has methods) use it, otherwise the inner enum
			return generator.ConvertFullName(Name) + "Enum";
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			// TODO if the created object is actually an enum (it can be a class in
			// case it has methods) use it, otherwise the inner enum
			return generator.ConvertFullName(Name) + "Enum";
		}
		#endregion
	}
}

