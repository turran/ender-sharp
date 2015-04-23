using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;

namespace Ender
{
	public enum ItemType
	{
		INVALID,
		BASIC,
		FUNCTION,
		ATTR,
		ARG,
		OBJECT,
		STRUCT,
		CONSTANT,
		ENUM,
		DEF,
	}

	public enum ItemTransfer
	{
		FULL,
		NONE,
		CONTAINER,
		CONTENT,
	}

	public class Item : IDisposable
	{
		// ender_item.h
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_ref(IntPtr i);
		[DllImport("libender.dll")]
		static extern void ender_item_unref(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_name_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern string ender_item_full_name_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern ItemType ender_item_type_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_parent_get(IntPtr i);
		//static extern const char * ender_item_type_name_get(Ender_Item_Type type);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_lib_get(IntPtr i);
		//static extern Eina_Bool ender_item_is_exception(Ender_Item *i);

		// Track whether Dispose has been called.
		private bool disposed = false;
		// Our private handle
		protected IntPtr raw;

		internal Item()
		{
		}

		internal Item(IntPtr p) : this(p, true)
		{
		}

		// This is going to be accesible from outside when creating
		// items on the fly, we need to make it public
		public Item(IntPtr p, bool owned)
		{
			raw = p;
			if (!owned)
				ender_item_ref(p);
		}

		public string Name
		{
			get {
				IntPtr uname = ender_item_name_get(raw);
				string s = Marshal.PtrToStringAnsi(uname);
				return s;
			}
		}

		public string FullName
		{
			get {
				return ender_item_full_name_get(raw);
			}
		}

		public string Namespace
		{
			get {
				string[] split = Name.Split('.');
				return String.Join(".", split, 0, split.Length - 1);
			}
		}

		public string Identifier
		{
			get {
				string[] split = Name.Split('.');
				return split[split.Length - 1];
			}
		}

		public Item Parent
		{
			get {
				IntPtr i = ender_item_parent_get(raw);
				return Create(i);
			}
		}

		public ItemType Type
		{
			get {
				return ender_item_type_get(raw);
			}
		}

		static internal System.Type ItemTypeToSystemType(ItemType type)
		{
			switch (type)
			{
				case ItemType.INVALID:
					return typeof(object);

				case ItemType.BASIC:
					return typeof(Basic);

				case ItemType.FUNCTION:
					return typeof(Function);

				case ItemType.ATTR:
					return typeof(Item);

				case ItemType.ARG:
					return typeof(Arg);

				case ItemType.OBJECT:
					return typeof(Object);

				case ItemType.STRUCT:
					return typeof(Struct);

				case ItemType.CONSTANT:
					return typeof(Constant);

				case ItemType.ENUM:
					return typeof(Enum);

				case ItemType.DEF:
					return typeof(Def);

				default:
					return null;
			}
		}

		static internal Item Create(IntPtr p)
		{
			if (p == IntPtr.Zero)
				return null;

			// Create the element based on its type
			ItemType type = ender_item_type_get(p);
			switch (type)
			{
				case ItemType.BASIC:
					return new Basic(p);

				case ItemType.FUNCTION:
					return new Function(p);

				case ItemType.ATTR:
					return new Attr(p);

				case ItemType.ARG:
					return new Arg(p);

				case ItemType.OBJECT:
					return new Object(p);

				case ItemType.STRUCT:
					return new Struct(p);

				case ItemType.CONSTANT:
					return new Constant(p);

				case ItemType.ENUM:
					return new Enum(p);

				case ItemType.DEF:
					return new Def(p);

				case ItemType.INVALID:
				default:
				break;
			}
			return null;
		}

		~Item()
		{
			Dispose(false);
		}

		public virtual CodeStatementCollection ManagedPreStatements(Generator generator,
				string varName, ArgDirection direction,
				ItemTransfer transfer)
		{
			return null;
		}

		public virtual CodeStatementCollection ManagedPostStatements(Generator generator,
				string varName, ArgDirection direction,
				ItemTransfer transfer)
		{
			return null;
		}

		public virtual CodeStatementCollection UnmanagedPreStatements(Generator generator,
				string varName, ArgDirection direction,
				ItemTransfer transfer)
		{
			return null;
		}

		public virtual CodeStatementCollection UnmanagedPostStatements(Generator generator,
				string varName, ArgDirection direction,
				ItemTransfer transfer)
		{
			return null;
		}

		public virtual string ManagedType(Generator generator)
		{
			return typeof(IntPtr).ToString();
		}

		public virtual string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			return typeof(IntPtr).ToString();
		}


		public virtual string UnmanagedName(string name)
		{
			return name;
		}

		public virtual CodeExpression Construct(Generator generator,
				string from, ArgDirection direction, ItemTransfer transfer)
		{
			return new CodeVariableReferenceExpression(from);
		}

		public virtual CodeObject Generate(Generator generator)
		{
			return null;
		}

		#region IDisposable
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				// Dispose managed data
				if (disposing)
				{

				}

				// Dispose unmanaged data
				if (raw != IntPtr.Zero)
				{
					ender_item_unref(raw);
					raw = IntPtr.Zero;
				}
				disposed = true;
			}
		}
		#endregion
	}
}
