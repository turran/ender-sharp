using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public class Object : Item
	{
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_inherit_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_ctor_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_functions_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_props_get(IntPtr i);

		internal Object()
		{
		}

		internal Object(IntPtr p) : this(p, true)
		{
		}

		public Object(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public Object Inherit
		{
			get {
				IntPtr inherit = ender_item_object_inherit_get(raw);
				if (inherit == IntPtr.Zero)
					return null;
				return new Object(inherit);
			}
		}
		public List Ctors
		{
			get {
				IntPtr l = ender_item_object_ctor_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		public List Functions
		{
			get {
				IntPtr l = ender_item_object_functions_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Function), true, true);
				return list;
			}
		}

		public List Props
		{
			get {
				IntPtr l = ender_item_object_props_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Attr), true, true);
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
			if (direction == ArgDirection.IN)
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

		public override CodeStatementCollection ManagedPostStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			if (direction == ArgDirection.OUT)
			{
				string rawName = varName + "Raw"; 
				// varName = new identifier(varNameRaw, false/true);
				CodeStatementCollection csc = new CodeStatementCollection();
				CodeStatement cs = new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
						new CodeObjectCreateExpression(ManagedType(generator),
							new CodeExpression[] {
								new CodeVariableReferenceExpression(rawName),
								new CodePrimitiveExpression(false)
							}));
				csc.Add(cs);
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
		#endregion
	}
}
