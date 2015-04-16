using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public class Struct : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_size_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_fields_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_struct_functions_get(IntPtr i);

		internal Struct()
		{
		}

		internal Struct(IntPtr p) : this(p, true)
		{
		}

		public Struct(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public long Size
		{
			get {
				IntPtr i = ender_item_struct_size_get(raw);
				unsafe
				{
					if (sizeof(IntPtr) == 4)
						return i.ToInt32();
					else
						return i.ToInt64();
				}
			}
		}

		public List Fields
		{
			get {
				IntPtr l = ender_item_struct_fields_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Attr), true, true);
				return list;
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_struct_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		#region Item interface
		public override CodeStatementCollection ManagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			if (direction == ArgDirection.OUT)
			{
				csc.Add(new CodeAssignStatement(
						new CodeVariableReferenceExpression(varName),
						new CodeObjectCreateExpression(
							new CodeTypeReference(generator.ConvertFullName(Name)))));
			}
			else
			{
				string rawName = varName + "Raw";
				csc.Add(new CodeVariableDeclarationStatement(typeof(IntPtr), rawName));
				// if varName == null varNameRaw = IntPtr.Zero : varNameRaw = varName.Raw
				CodeStatement cs = new CodeConditionStatement(
						new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(varName),
							CodeBinaryOperatorType.IdentityEquality,
							new CodePrimitiveExpression(null)),
								new CodeStatement[] {
									new CodeAssignStatement(new CodeVariableReferenceExpression(rawName),
									new CodeTypeReferenceExpression("IntPtr.Zero"))
								},
							new CodeStatement[] {
								new CodeAssignStatement(new CodeVariableReferenceExpression(rawName),
									new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "Raw"))
							}
						);
				csc.Add(cs);
			}
			return csc;
		}

		public override CodeStatementCollection ManagedPostStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			return null;
		}
		#endregion
	}
}
