using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Function : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_args_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_ret_get(IntPtr i);
/*
EAPI Ender_Item * ender_item_function_args_at(Ender_Item *i, int idx);
EAPI int ender_item_function_args_count(Ender_Item *i);
Eina_Bool ender_item_function_call(Ender_Item *i,
		Ender_Value *args, Ender_Value *retval);
EAPI int ender_item_function_flags_get(Ender_Item *i);
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
				return new Arg(i, true);
			}
		}
	}
}


