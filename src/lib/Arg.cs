using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public enum ArgDirection
	{
		IN,
		OUT,
		IN_OUT		
	}

	[Flags]
	public enum ArgFlag
	{
		RETURN = 1,
		CLOSURE = 2,
	}

	public class Arg : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_arg_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern ArgDirection ender_item_arg_direction_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern ArgFlag ender_item_arg_flags_get(IntPtr i);
/*
EAPI Ender_Item_Transfer ender_item_arg_transfer_get(Ender_Item *i);
*/

		internal Arg()
		{
		}

		internal Arg(IntPtr p) : this(p, true)
		{
		}

		public Arg(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item ArgType {
			get {
				IntPtr t = ender_item_arg_type_get(raw);
				return Item.Create(t);
			}
		}

		public ArgDirection Direction {
			get {
				return ender_item_arg_direction_get(raw);
			}
		}

		public ArgFlag Flags {
			get {
				return ender_item_arg_flags_get(raw);
			}
		}
	}
}