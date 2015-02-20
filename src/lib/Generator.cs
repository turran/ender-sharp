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
		static extern IntPtr ender_lib_item_find(IntPtr lib, string name);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_item_list(IntPtr lib, ItemType type);
		//static extern int ender_lib_version_get(const Ender_Lib *thiz);
		[DllImport("libender.dll")]
		static extern string ender_lib_name_get(IntPtr lib);
		[DllImport("libender.dll")]
		static extern IntPtr ender_lib_dependencies_get(IntPtr lib);
		//static extern Ender_Item * ender_lib_item_find(const Ender_Lib *thiz, const char *name);
		/* ender_item.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_ref(IntPtr i);
		[DllImport("libender.dll")]
		static extern void ender_item_unref(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_name_get(IntPtr i);
		//static extern Ender_Item_Type ender_item_type_get(Ender_Item *thiz);
		//static extern Ender_Item * ender_item_parent_get(Ender_Item *thiz);
		//static extern const char * ender_item_type_name_get(Ender_Item_Type type);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_lib_get(IntPtr i);
		//static extern Eina_Bool ender_item_is_exception(Ender_Item *i);
		/* ender_item_object.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_object_inherit_get(IntPtr i);
		/* ender_utils.h */
		[DllImport("libender.dll")]
		static extern string ender_utils_name_convert(string s, Case scase, Notation snot,
				Case dcase, Notation dnot);

		private static string camelize(string str)
		{
			string[] splitByPt = str.Split('.');
			string ret = null;
 
			foreach(string wordByPt in splitByPt)
			{
				string result = "";

				string[] splitByUnder = wordByPt.Split('_');
				foreach(string word in splitByUnder)
				{
					result += word.Substring(0, 1).ToUpper() + word.Substring(1);
				}
				if (ret == null)
					ret = result;
				else
					ret += "." + result;

			}
			return ret;
		}

		private static string camelize(string[] strArray)
		{
			string result = "";
			foreach(string word in strArray)
			{
				result += word.Substring(0, 1).ToUpper() + word.Substring(1);
			}
			return result;

		}

		private CodeNamespace createNamespace(CodeCompileUnit cu, string name, string item_name)
		{
			CodeNamespace ns = null;
			for (int i = 0; i < cu.Namespaces.Count; i++)
			{
				CodeNamespace c = cu.Namespaces[i];
				if (c.Name == name)
				{
					ns = c;
					break;
				}
			}
			if (ns == null)
			{
				Console.WriteLine("Creating namespace, name: " + name + " ender_name: " + item_name);
				ns = new CodeNamespace(name);
				ns.UserData["enderName"] = item_name;
				cu.Namespaces.Add(ns);
			}
			return ns;
		}

		private string getItemName(IntPtr item)
		{
			IntPtr uname = ender_item_name_get(item);
			string name = Marshal.PtrToStringAnsi(uname);
			return name;
		}

		private void addObject(CodeObject parent, CodeTypeDeclaration child)
		{
			if (child == null)
				return;
			if (parent.GetType() == typeof (CodeNamespace))
			{
				CodeNamespace ns = (CodeNamespace)parent;
				ns.Types.Add(child);
			}
			else if (parent.GetType() == typeof (CodeTypeDeclaration))
			{
				CodeTypeDeclaration td = (CodeTypeDeclaration)parent;
				td.Members.Add(child);
			}
			else
			{
				Console.WriteLine("[ERROR]");
			}
		}

		/* for a given item, generate all the namespaces and items
		 * on the name hierarchy
		 * foo.bar.myobject => ns(foo) -> ns(bar) -> object(MyObject)
		 */
		private CodeObject generateParentObjects(CodeCompileUnit cu, IntPtr item)
		{
			IntPtr lib = ender_item_lib_get(item);
			IntPtr uname = ender_item_name_get(item);
			CodeObject parent = cu.Namespaces[0];
			string name = Marshal.PtrToStringAnsi(uname);
			string last_level = null;
			string last_levelName = null;

			string[] levels = name.Split('.');
			for (int i = 0; i < levels.Length - 1; i++)
			{
				string level = levels[i];
				string current_level;
				string current_levelName;

				if (last_level != null)
				{
					current_level = last_level + "." + level;
					current_levelName = last_levelName + "." + camelize(level);
				}
				else
				{
					current_level = level;
					current_levelName = camelize(level);
				}

				IntPtr levelPtr = ender_lib_item_find(lib, current_level);
				/* The item does not exist, create a new 
				 * namespace or a non instantiable class */
				if (levelPtr == IntPtr.Zero)
				{
					if (parent.GetType() == typeof(CodeNamespace))
					{
						CodeNamespace ns = createNamespace(cu, current_levelName, current_level);
						parent = ns;
					}
					else
					{
						/* create an empty class */
						CodeTypeDeclaration o = new CodeTypeDeclaration(current_levelName);
						CodeTypeDeclaration td = (CodeTypeDeclaration)parent;
						td.Members.Add(o);
						parent = o;				
					}
				}
				else
				{
					CodeTypeDeclaration o = generateItem(cu, levelPtr);
					addObject(parent, o);
					ender_item_unref(levelPtr);
					if (o != null)
						parent = o;
				}
				last_level = current_level;
				last_levelName = current_levelName;
			}
			return parent;
		}

		private void generateObject(CodeCompileUnit cu, IntPtr item)
		{
			/* generate all the namespace in case it does not exists */
			CodeObject parent = generateParentObjects(cu, item);
			string name = getItemName(item);
			Console.WriteLine ("parent = " + parent);
			string diff_name = name.Substring(((string)parent.UserData["enderName"]).Length + 1);

			/* generate the new name */
			Console.WriteLine("diff name = " + diff_name);
			CodeTypeDeclaration o = new CodeTypeDeclaration(camelize(diff_name));
			addObject(parent, o);
			/* add the base type */
			IntPtr inherit = ender_item_object_inherit_get(item);
			if (inherit != IntPtr.Zero)
			{
				o.BaseTypes.Add(getItemName(inherit));
				ender_item_unref(inherit);
			}
		}

		private void generateLib(CodeCompileUnit cu, IntPtr lib)
		{
			CodeNamespace root = new CodeNamespace();
			cu.Namespaces.Add(root);
			/* add the imports/using */
			List list = new List(ender_lib_dependencies_get(lib));
			foreach (IntPtr l in list)
			{
				string dname = ender_lib_name_get(l);
				root.Imports.Add(new CodeNamespaceImport(camelize(dname)));
			}
			/* TODO add a method on ender to free the dependency list? */
			/* add the new namespace */
		}

		private CodeTypeDeclaration generateItem(CodeCompileUnit cu, IntPtr item)
		{
			/* TODO check if the item has been already processed */
			Console.WriteLine("ERROR: generating new item");
			return null;
		}

		public CodeCompileUnit generate(string nsName)
		{
			/* get the ender library */
			IntPtr lib = ender_lib_find(nsName);
			if (lib == IntPtr.Zero)
				return null;
			CodeCompileUnit cu = new CodeCompileUnit();
			/* first set the information from the library itself */
			generateLib(cu, lib);

			List list = new List(ender_lib_item_list(lib, ItemType.OBJECT));
			foreach (IntPtr item in list)
			{
				generateObject(cu, item);
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
