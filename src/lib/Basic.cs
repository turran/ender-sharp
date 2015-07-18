using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public class Basic : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		extern static ValueType ender_item_basic_value_type_get(IntPtr i);

		internal Basic()
		{
		}

		internal Basic(IntPtr p) : this(p, true)
		{
		}

		public Basic(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public ValueType ValueType
		{
			get {
				return ender_item_basic_value_type_get(raw);
			}
		}

		#region Item interface
		public override CodeStatementCollection ManagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			CodeStatementCollection csc = null;
			if (ValueType == ValueType.STRING &&
					transfer == ItemTransfer.NONE &&
					direction == ArgDirection.OUT)
			{
				string rawName = varName + "Raw"; 

				csc = new CodeStatementCollection();
				csc.Add(new CodeVariableDeclarationStatement(typeof(IntPtr), rawName));
			}
			return csc;
		}

		public override CodeStatementCollection ManagedPostStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			CodeStatementCollection csc = null;
			if (ValueType == ValueType.STRING &&
					transfer == ItemTransfer.NONE &&
					direction == ArgDirection.OUT)
			{
				string rawName = varName + "Raw"; 

				csc = new CodeStatementCollection();
				CodeStatement cs = new CodeAssignStatement(new CodeVariableReferenceExpression(varName),
						Construct(generator, rawName, direction, transfer));
				csc.Add(cs);
			}
			return csc;

		}

		public override string UnmanagedName(string name,
				ArgDirection direction, ItemTransfer transfer)
		{
			if (ValueType == ValueType.STRING &&
					transfer == ItemTransfer.NONE &&
					direction == ArgDirection.OUT)
			{
				return name + "Raw";
			}
			else
			{
				return name;
			}
		}

		public override string ManagedType(Generator generator)
		{
			System.Type st;
			switch (ValueType)
			{
				case ValueType.BOOL:
					st = typeof(bool);
					break;
				case ValueType.UINT8:
					st = typeof(byte);
					break;
				case ValueType.INT8:
					st = typeof(sbyte);
					break;
				case ValueType.UINT32:
					st = typeof(uint);
					break;
				case ValueType.INT32:
					st = typeof(int);
					break;
				case ValueType.UINT64:
					st = typeof(ulong);
					break;
				case ValueType.INT64:
					st = typeof(long);
					break;
				case ValueType.DOUBLE:
					st = typeof(double);
					break;
				case ValueType.STRING:
					st = typeof(string);
					break;
				case ValueType.POINTER:
					st = typeof(IntPtr);
					break;
				case ValueType.SIZE:
					st = typeof(IntPtr);
					break;
				default:
					st = typeof(object);
					break;
			}
			return st.ToString();
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			// The special case for in, full char *
			if (ValueType == ValueType.STRING)
			{
				if (transfer == ItemTransfer.FULL
					&& direction == ArgDirection.IN)
					return typeof(IntPtr).ToString();
				if (transfer == ItemTransfer.NONE
					&& direction == ArgDirection.OUT)
					return typeof(IntPtr).ToString();
			}	
			return ManagedType(generator);
		}

		public override CodeExpression Construct(Generator generator,
				string from, ArgDirection direction, ItemTransfer transfer)
		{
			// The special case for in, full char *
			if (ValueType == ValueType.STRING && transfer == ItemTransfer.NONE
					&& direction == ArgDirection.OUT)
			{
				return new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"),
					"PtrToStringAnsi",
					new CodeVariableReferenceExpression(from));
			}
			else
			{
				return new CodeVariableReferenceExpression(from);
			}	
		}

		public override CodeObject Generate(Generator generator)
		{
			System.Type st;
			switch (ValueType)
			{
				case ValueType.BOOL:
					st = typeof(bool);
					break;
				case ValueType.UINT8:
					st = typeof(byte);
					break;
				case ValueType.INT8:
					st = typeof(sbyte);
					break;
				case ValueType.UINT32:
					st = typeof(uint);
					break;
				case ValueType.INT32:
					st = typeof(int);
					break;
				case ValueType.UINT64:
					st = typeof(ulong);
					break;
				case ValueType.INT64:
					st = typeof(long);
					break;
				case ValueType.DOUBLE:
					st = typeof(double);
					break;
				case ValueType.STRING:
					st = typeof(string);
					break;
				case ValueType.POINTER:
					st = typeof(IntPtr);
					break;
				case ValueType.SIZE:
					st = typeof(IntPtr);
					break;
				default:
					st = typeof(object);
					break;
			}
			return new CodeTypeReference(st);
		}
		#endregion
	}
}
