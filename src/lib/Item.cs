using System;
using System.Collections;
using System.Runtime.InteropServices;

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
		static extern ItemType ender_item_type_get(IntPtr i);
		//static extern Ender_Item * ender_item_parent_get(Ender_Item *thiz);
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
				case ItemType.FUNCTION:
				case ItemType.ATTR:
				case ItemType.ARG:
					return typeof(Item);

				case ItemType.OBJECT:
					return typeof(Object);

				case ItemType.STRUCT:
				case ItemType.CONSTANT:
				case ItemType.ENUM:
				case ItemType.DEF:
					return typeof(Item);

				default:
					return null;
			}
		}

		static internal Item Create(IntPtr p)
		{
			// Create the element based on its type
			ItemType type = ender_item_type_get(p);
			switch (type)
			{
				case ItemType.INVALID:
				break;
				case ItemType.BASIC:
				break;
				case ItemType.FUNCTION:
				break;
				case ItemType.ATTR:
				break;
				case ItemType.ARG:
				break;

				case ItemType.OBJECT:
					return new Object(p);

				case ItemType.STRUCT:
				break;
				case ItemType.CONSTANT:
				break;
				case ItemType.ENUM:
				break;
				case ItemType.DEF:
				break;
				default:
				break;
			}
			return null;
		}

		~Item()
		{
			Dispose(false);
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
