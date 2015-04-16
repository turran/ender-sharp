using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public class Def : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_def_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_def_functions_get(IntPtr i);
		internal Def()
		{
		}

		internal Def(IntPtr p) : this(p, true)
		{
		}

		public Def(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Item DefType {
			get {
				IntPtr t = ender_item_def_type_get(raw);
				return Item.Create(t);
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_def_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		public Item FinalDefType {
			get {
				Item i = DefType;
				if (i is Def)
				{
					Item ret = ((Def)i).FinalDefType;
					i.Dispose ();
					return ret;
				}
				else
				{
					return i;
				}
			}
		}

		#region Item interface
		public override CodeStatementCollection ManagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = FinalDefType;
			// Can not convert implicitly for out parameters
			if (i.Type == ItemType.BASIC && direction == ArgDirection.OUT)
			{
				string rawName = varName + "Raw";
				CodeStatementCollection csc = new CodeStatementCollection();
				csc.Add(new CodeVariableDeclarationStatement(i.ManagedType(generator), rawName));
				return csc;
			}
			else
			{
				return i.ManagedPreStatements(generator, varName, direction, transfer);
			}
		}

		public override CodeStatementCollection ManagedPostStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = FinalDefType;
			if (i.Type == ItemType.BASIC && direction == ArgDirection.OUT)
			{
				string rawName = varName + "Raw";
				CodeStatementCollection csc = new CodeStatementCollection();
				csc.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
						new CodeVariableReferenceExpression(rawName)));
				return csc;
			}
			else
			{
				return null;
			}
		}

		public override string ManagedType(Generator generator)
		{
			return generator.ConvertFullName(Name);
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			Item i = FinalDefType;
			return i.UnmanagedType(generator, direction, transfer);
		}
		#endregion
	}
}

