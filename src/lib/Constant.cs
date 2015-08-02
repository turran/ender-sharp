using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Constant : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_constant_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_constant_value_get(IntPtr i, IntPtr v);

		internal Constant()
		{
		}

		internal Constant(IntPtr p) : this(p, true)
		{
		}

		public Constant(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item ConstantType
		{
			get {
				IntPtr t = ender_item_constant_type_get(raw);
				return Item.Create(t);
			}
		}

		public Value Value
		{
			get {
				IntPtr valueRaw = Value.CreateRaw();
				ender_item_constant_value_get(raw, valueRaw);
				Value v = new Value(valueRaw, true);
				return v;
			}
		}

		#region Item interface
		public override string UnmanagedName(string name,
				ArgDirection direction, ItemTransfer transfer)
		{
			return ConstantType.UnmanagedName(name, direction, transfer);
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			return ConstantType.UnmanagedType(generator, direction, transfer);
		}
		#endregion
	}
}

