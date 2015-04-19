using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	[Flags]
	public enum FunctionFlag
	{
		IS_METHOD = (1 << 0),
		THROWS    = (1 << 1),
		CTOR      = (1 << 2),
		REF       = (1 << 3),
		UNREF     = (1 << 4),
		CALLBACK  = (1 << 5),
		VALUE_OF  = (1 << 6),
	}

	public class Function : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_args_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_ret_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern FunctionFlag ender_item_function_flags_get(IntPtr i);
/*
EAPI Ender_Item * ender_item_function_args_at(Ender_Item *i, int idx);
EAPI int ender_item_function_args_count(Ender_Item *i);
Eina_Bool ender_item_function_call(Ender_Item *i,
		Ender_Value *args, Ender_Value *retval);
EAPI int ender_item_function_throw_position_get(Ender_Item *i);
*/
		internal Function()
		{
		}

		internal Function(IntPtr p) : this(p, true)
		{
		}

		public Function(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public List Args
		{
			get {
				IntPtr l = ender_item_function_args_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Arg), true, true);
				return list;
			}
		}

		public Arg Ret
		{
			get {
				IntPtr i = ender_item_function_ret_get(raw);
				if (i == IntPtr.Zero)
					return null;

				return new Arg(i, true);
			}
		}

		public FunctionFlag Flags
		{
			get {
				return ender_item_function_flags_get(raw);
			}
		}

		#region Item interface
		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			return generator.ConvertFullName(Name) + "Internal";
		}

		public override string UnmanagedName(string name)
		{
			return name;
		}
		#endregion
	}
}


