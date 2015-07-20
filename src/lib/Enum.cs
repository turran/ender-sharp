using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public class Enum : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_enum_values_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_enum_functions_get(IntPtr i);

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

		public List Functions
		{
			get {
				IntPtr l = ender_item_enum_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		#region Item interface
		public override string ManagedType(Generator generator)
		{
			List funcs = Functions;
			string ret = generator.ConvertFullName(Name) + "Enum";
			if (funcs != null)
				ret += ".Enum";
			return ret;
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			return ManagedType(generator);
		}
		#endregion
	}
}

