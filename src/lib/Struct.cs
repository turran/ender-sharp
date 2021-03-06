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
			string rawName = varName + "Raw";
			csc.Add(new CodeVariableDeclarationStatement(typeof(IntPtr), rawName));

			if (direction == ArgDirection.OUT)
			{
				csc.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(rawName),
					new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(
						ManagedType(generator)), "CreateRaw")
					));
			}
			else
			{
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
		// Generate the pre statement in the form:
		// if ((rRaw == IntPtr.Zero)) {
		//     r = null;
		// }
		// else {
		//     r = new Enesim.Matrix ();
		//     r.Raw = rRaw;
		// }
		public override CodeStatementCollection UnmanagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			CodeStatementCollection csc = new CodeStatementCollection();

			csc.Add(new CodeVariableDeclarationStatement(ManagedType(generator), varName));
			if (direction == ArgDirection.IN)
			{
				string rawName = varName + "Raw"; 
				CodeStatement cs = new CodeConditionStatement(
						new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(rawName),
							CodeBinaryOperatorType.IdentityEquality,
							new CodeTypeReferenceExpression("IntPtr.Zero")),
								new CodeStatement[] {
									new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
									new CodePrimitiveExpression(null))
								},
								new CodeStatement[] {
									new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
									Construct(generator, rawName, direction, transfer))
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
			CodeStatementCollection csc = new CodeStatementCollection();
			string rawName = varName + "Raw";
			if (direction == ArgDirection.IN)
			{
				// if varNameRaw != IntPtr.Zero; Marshal.FreeHGlobal 
				CodeStatement cs = new CodeConditionStatement(
						new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(rawName),
							CodeBinaryOperatorType.IdentityInequality,
							new CodeTypeReferenceExpression("IntPtr.Zero")),
								new CodeStatement[] {
									new CodeExpressionStatement(new CodeMethodInvokeExpression(
										new CodeTypeReferenceExpression("Marshal"),
										"FreeHGlobal", new CodeVariableReferenceExpression(rawName)))
								},
							new CodeStatement[] {
							}
						);
				csc.Add(cs);
			}
			else
			{
				csc.Add(new CodeAssignStatement(
						new CodeVariableReferenceExpression(varName),
						new CodeObjectCreateExpression(
							new CodeTypeReference(ManagedType(generator)))));
				csc.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression(varName), "Raw"), new CodeVariableReferenceExpression(rawName)));
			}
			return csc;
		}

		// FullName
		public override string ManagedType(Generator generator)
		{
			return FullQualifiedName;
		}

		public override string UnmanagedName(string name,
				ArgDirection direction, ItemTransfer transfer)
		{
			return name + "Raw";
		}

		// new FullName(from, incRef);
		public override CodeExpression Construct(Generator generator,
				string from, string type, ArgDirection direction,
				ItemTransfer transfer)
		{
			bool incRef = false;

			if (direction == ArgDirection.IN)
			{
				if (transfer == ItemTransfer.FULL)
					incRef = true;
				else
					incRef = false;
			}
			return new CodeObjectCreateExpression(type,
					new CodeExpression[] {
						new CodeVariableReferenceExpression(from),
						new CodePrimitiveExpression(incRef)
					});
		}

		#endregion
	}
}
