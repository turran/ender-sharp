using System;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Lib
	{
		/* ender_lib.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_find(string name);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_item_find(IntPtr lib, string name);
		[DllImport("libender.dll")]
		static extern int ender_lib_version_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_name_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern Utils.Notation ender_lib_notation_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern Utils.Case ender_lib_case_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_item_list(IntPtr lib, ItemType type);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_dependencies_get(IntPtr lib);

		private IntPtr raw;

		public int Version
		{
			get {
				return ender_lib_version_get(raw);
			}
		}

		public string Name
		{
			get {
				IntPtr uname = ender_lib_name_get(raw);
				string s = Marshal.PtrToStringAnsi(uname);
				return s;
			}
		}

		public Utils.Case Case
		{
			get {
				return ender_lib_case_get(raw);
			}
		}

		public Utils.Notation Notation
		{
			get {
				return ender_lib_notation_get(raw);
			}
		}

		private Lib()
		{
		}

		public Lib(IntPtr p, bool owned) : this(p)
		{
		}

		public Lib(IntPtr p)
		{
			raw = p;
		}

		public Item FindItem(string name)
		{
			IntPtr i = ender_lib_item_find(raw, name);
			if (i == IntPtr.Zero)
				return null;
			Item ret = Item.Create(i);
			return ret;
		}

		public List List(ItemType type)
		{
			IntPtr i = ender_lib_item_list(raw, type);
			List ret = new List(i, Item.ItemTypeToSystemType(type), true, true);
			return ret;
		}

		public List Dependencies
		{
			get {
				IntPtr l = ender_lib_dependencies_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Lib), true, false);
				return list;
			}
		}

		static public Lib Find(string name)
		{
			IntPtr p = ender_lib_find(name);
			if (p == IntPtr.Zero)
				return null;
			Lib lib = new Lib(p);
			return lib;
		}
	}
}

