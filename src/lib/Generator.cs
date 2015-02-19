using System;
using System.Collections;
using System.CodeDom;
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

	public enum Case
	{
		CAMEL,
		PASCAL,
		UNDERSCORE,
	}

	public enum Notation
	{
		ENGLISH,
		LATIN,
	}

	public class Generator
	{
		/* ender_main.h */
		[DllImport("libender.dll")]
		static extern void ender_init();
		[DllImport("libender.dll")]
		static extern void ender_shutdown();
		/* ender_lib.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_find(string name);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_item_list(IntPtr lib, ItemType type);
		//static extern int ender_lib_version_get(const Ender_Lib *thiz);
		[DllImport("libender.dll")]
		static extern string ender_lib_name_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_dependencies_get(IntPtr lib);
		//static extern Ender_Item * ender_lib_item_find(const Ender_Lib *thiz, const char *name);
		/* ender_item.h */
		//static extern Ender_Item * ender_item_ref(Ender_Item *thiz);
		//static extern void ender_item_unref(Ender_Item *thiz);
		[DllImport("libender.dll")]
		static extern string ender_item_name_get(IntPtr i);
		//static extern Ender_Item_Type ender_item_type_get(Ender_Item *thiz);
		//static extern Ender_Item * ender_item_parent_get(Ender_Item *thiz);
		//static extern const char * ender_item_type_name_get(Ender_Item_Type type);
		//static extern const Ender_Lib * ender_item_lib_get(Ender_Item *thiz);
		//static extern Eina_Bool ender_item_is_exception(Ender_Item *i);
		/* ender_utils.h */
		[DllImport("libender.dll")]
		static extern string ender_utils_name_convert(string s, Case scase, Notation snot,
				Case dcase, Notation dnot);

		private void generateObject(CodeNamespace ns, IntPtr item)
		{
			/*
			String name = ender_utils_name_convert(
					ender_item_name_get(item),
					Case.UNDERSCORE,
					Notation.LATIN, Case.CAMEL,
					Notation.ENGLISH);
			*/
			String name = ender_item_name_get(item);
			CodeTypeDeclaration o = new CodeTypeDeclaration(name);
			ns.Types.Add(o);
		}

		private CodeNamespace generateLib(CodeCompileUnit cu, IntPtr lib)
		{
			CodeNamespace root = new CodeNamespace();
			cu.Namespaces.Add(root);
			/* add the imports/using */
			List list = new List(ender_lib_dependencies_get(lib));
			foreach (IntPtr l in list)
			{
				/*
				String name = ender_utils_name_convert(
						ender_lib_name_get(l),
						Case.UNDERSCORE,
						Notation.LATIN, Case.CAMEL,
						Notation.ENGLISH);
				*/
				String name = ender_lib_name_get(l);
				root.Imports.Add(new CodeNamespaceImport(name));
			}
			/* TODO add a method on ender to free the dependency list? */
			/* add the new namespace */
			CodeNamespace ns = new CodeNamespace(ender_lib_name_get(lib));
			cu.Namespaces.Add(ns);
			return ns;
		}

		private void generateItem(IntPtr item)
		{

		}

		public CodeCompileUnit generate(string nsName)
		{
			/* get the ender library */
			IntPtr lib = ender_lib_find(nsName);
			if (lib == IntPtr.Zero)
				return null;
			CodeCompileUnit cu = new CodeCompileUnit();
			/* first set the information from the library itself */
			CodeNamespace ns = generateLib(cu, lib);

			List list = new List(ender_lib_item_list(lib, ItemType.OBJECT));
			foreach (IntPtr item in list)
			{
				generateObject(ns, item);
			}
			/* TODO add a method on ender to free an item based list */
			/* iterate over every object */
			/* jump to root nodes, ie. items that do not have inheritances */
			/* in the mean time add every non root node into a queue */
			/* iterate the childs and remove theme from the queue in case it is already processed */
			/* finally continue with the queue again */
			return cu;
		}

		/* initialize ender before we do anything */
		static Generator()
		{
			ender_init();
		}
	}

	/* our match to an Eina List */
	internal class List : IEnumerable
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct EinaList
		{
			public IntPtr data;
			public IntPtr next;
			public IntPtr prev;
			public IntPtr accounting;
		}

		private IntPtr _raw;

		private static EinaList toEinaList(IntPtr l)
		{
			EinaList list = (EinaList)Marshal.PtrToStructure(l, typeof(EinaList));
			return list;
		}

		public List(IntPtr l)
		{
			_raw = l;
		}

		public IntPtr raw
		{
			get {
				return _raw;
			}
		}

		public IntPtr data
		{
			get {
				EinaList l = toEinaList(_raw);
				return l.data;
			}
		}

		public IEnumerator GetEnumerator()
		{
			IntPtr ptr = _raw;

			while (ptr != IntPtr.Zero)
			{
				EinaList l = toEinaList(ptr);
				yield return l.data;
				ptr = l.next;
			}
		}
	}
}
