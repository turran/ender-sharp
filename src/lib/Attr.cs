using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	[Flags]
	public enum AttrFlag
	{
		VALUE_OF = 1,
		DOWNCAST = 2,
		NULLABLE = 4,
	}

	public class Attr : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_attr_offset_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_attr_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern AttrFlag ender_item_attr_flags_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_attr_getter_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_attr_setter_get(IntPtr i);

		internal Attr()
		{
		}

		internal Attr(IntPtr p) : this(p, true)
		{
		}

		public Attr(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public AttrFlag Flags {
			get {
				return ender_item_attr_flags_get(raw);
			}
		}

		public Item AttrType {
			get {
				IntPtr t = ender_item_attr_type_get(raw);
				return Item.Create(t);
			}
		}

		public long Offset
		{
			get {
				IntPtr i = ender_item_attr_offset_get(raw);
				unsafe
				{
					if (sizeof(IntPtr) == 4)
						return i.ToInt32();
					else
						return i.ToInt64();
				}
			}
		}

		public Function Getter
		{
			get {
				IntPtr i = ender_item_attr_getter_get(raw);
				if (i != IntPtr.Zero)
					return new Function(i, false);
				else
					return null;
			}
		}

		public Function Setter
		{
			get {
				IntPtr i = ender_item_attr_setter_get(raw);
				if (i != IntPtr.Zero)
					return new Function(i, false);
				else
					return null;
			}
		}

		#region Item interface
		public override string ManagedType(Generator generator)
		{
			Item i = AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return "System.IntPtr";
			}
			else
			{
				return i.ManagedType(generator);
			}
		}
		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return "System.IntPtr";
			}
			else
			{
				return i.UnmanagedType(generator, direction, transfer);
			}
		}
		public override CodeExpression Construct(Generator generator,
				string from, ArgDirection direction, ItemTransfer transfer)
		{
			Item i = AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return new CodeVariableReferenceExpression(from);
			}
			else
			{
				return i.Construct(generator, from, direction, transfer);
			}
		}
		public override CodeStatementCollection ManagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return null;
			}
			else
			{
				return i.ManagedPreStatements(generator, varName, direction, transfer);
			}

		}
		public override string UnmanagedName(string name,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return null;
			}
			else
			{
				return i.UnmanagedName(name, direction, transfer);
			}
		}
		#endregion
	}
}

