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
		[DllImport("libender.dll")]
		static extern bool ender_item_object_ref(IntPtr i, IntPtr o);
		[DllImport("libender.dll")]
		static extern bool ender_item_object_unref(IntPtr i, IntPtr o);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_downcast(IntPtr i, IntPtr o);

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

		public bool Ref(IntPtr o)
		{
			return ender_item_object_ref(raw, o);
		}

		public bool Unref(IntPtr o)
		{
			return ender_item_object_unref(raw, o);
		}

		public Item Downcast(IntPtr o)
		{
			IntPtr ret = ender_item_object_downcast(raw, o);
			if (ret == IntPtr.Zero)
				return null;
			else
				return Item.Create(ret);
		}

		#region Item interface
		// IntPtr rawName;
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
				CodeStatementCollection csc = new CodeStatementCollection();
				// if (varNameRaw == IntPtr.Zero)
				//  varName = null
				// else
				//  varName = new identifier(varNameRaw, false/true);
				//
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
				return csc;
			}
			else
			{
				return null;
			}
		}

		// Generate the pre statement in the form:
		// if ((rRaw == IntPtr.Zero)) {
		//     r = null;
		// }
		// else {
		//     r = new Enesim.Renderer(rRaw, false/true);
		// }
		public override CodeStatementCollection UnmanagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			ItemTransfer invTransfer;

			if (transfer == ItemTransfer.FULL)
				invTransfer = ItemTransfer.NONE;
			else
				invTransfer = ItemTransfer.FULL;

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
									Construct(generator, rawName, direction, invTransfer))
								}
						);
				csc.Add(cs);
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


		//      |      in    |    out    |
		//      |   F  |  N  |  F  |  N  |
		// func |   R  |  -  |  -  |  R  |
		// cb   |   -  |  R  |  R  |  -  |
		// new FullName(from, false);
		public override CodeExpression Construct(Generator generator,
				string from, ArgDirection direction, ItemTransfer transfer)
		{
			bool incRef = false;
			bool hasCtor = false;
			bool hasDowncast = false;
			List functions = Functions;


			if (direction == ArgDirection.IN)
			{
				if (transfer == ItemTransfer.FULL)
					incRef = true;
				else
					incRef = false;
			}

			// Check if there is no ctor to call the downcast instead
			// of the ctor directly
			if (functions != null)
			{
				foreach (Function f in functions)
				{
					if ((f.Flags & FunctionFlag.CTOR) == FunctionFlag.CTOR)
					{
						hasCtor = true;
						break;
					}

					if ((f.Flags & FunctionFlag.DOWNCAST) == FunctionFlag.DOWNCAST)
					{
						hasDowncast = true;
					}
				}
			}

			if (!hasCtor && hasDowncast)
			{
				return new CodeMethodInvokeExpression(
						new CodeMethodReferenceExpression(
								new CodeTypeReferenceExpression(ManagedType(generator)),
								"Downcast"),
						new CodeExpression[] {
							new CodeVariableReferenceExpression(from),
							new CodePrimitiveExpression(incRef)
						});
			}
			else
			{

				return new CodeObjectCreateExpression(ManagedType(generator),
						new CodeExpression[] {
							new CodeVariableReferenceExpression(from),
							new CodePrimitiveExpression(incRef)
						});
			}
		}
		#endregion
	}
}
