using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

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
		NULLABLE = 4,
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
		[DllImport("libender.dll")]
		static extern ItemTransfer ender_item_arg_transfer_get(IntPtr i);

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

		public ItemTransfer Transfer {
			get {
				return ender_item_arg_transfer_get(raw);
			}
		}

		public string GeneratePinvoke(Generator generator)
		{
			Item i = ArgType;

			if (i == null)
			{
				Console.WriteLine("[WRN] Arg '" + Name + "' without a type?");
				return "IntPtr " + Name;
			}

			string ret = i.UnmanagedType(generator, Direction, Transfer);
			// For structs, the out is irrelevant
			if (Direction == ArgDirection.OUT && i.Type != ItemType.STRUCT)
				ret = "out " + ret;
			ret += " " + generator.Provider.CreateValidIdentifier(i.UnmanagedName(Name));
			return ret;
		}

		public string GenerateRetPinvoke(Generator generator)
		{
			Item i = ArgType;
			if (i == null)
			{
				Console.WriteLine("[WRN] Arg '" + Name + "' without a type?");
				return "IntPtr";
			}

			string ret = i.UnmanagedType(generator, Direction, Transfer);
			return ret;
		}

		public CodeTypeReference GenerateRet(Generator generator)
		{
			Item i = ArgType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + Name + "' without a type?");
				return new CodeTypeReference("System.IntPtr");
			}

			return new CodeTypeReference(i.ManagedType(generator));
		}

		// The expression to pass into the Pinvoke method for this arg
		public CodeExpression GenerateExpression(Generator generator)
		{
			CodeVariableReferenceExpression ret;
			Item i = ArgType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Invalid arg " + Name);
				ret = new CodeVariableReferenceExpression();
				ret.VariableName = generator.Provider.CreateValidIdentifier(Name);
				if (Direction == ArgDirection.OUT)
					return new CodeDirectionExpression(FieldDirection.Out, ret);
				else
					return ret;
			}
			else
			{
				ret = new CodeVariableReferenceExpression(i.UnmanagedName(Name));
				if (Direction == ArgDirection.OUT && i.Type != ItemType.STRUCT)
					return new CodeDirectionExpression(FieldDirection.Out, ret);
				else
					return ret;
			}
		}
	}
}
