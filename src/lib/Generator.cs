using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;

namespace Ender
{
	public class Generator
	{
		private CodeCompileUnit cu;
		private CodeNamespace root;
		private Lib lib;
		private Dictionary<string, CodeObject> processed;

		private Generator()
		{
		}

		public Generator(string name)
		{
			// Create our empty compilation unit
			cu = new CodeCompileUnit();
			// Find the ender library
			lib = Lib.Find(name);
			// Our dictionary to keep processed items in sync
 			processed = new Dictionary<string, CodeObject>();
		}

		/* for a given item, generate all the namespaces and items
		 * on the name hierarchy
		 * foo.bar.myobject => ns(foo) -> ns(bar) -> object(MyObject)
		 */
		private CodeObject GenerateParentObjects(Item item)
		{
			CodeObject parent = null;
			string[] namespaces = item.Namespace.Split('.');
			int count = 0;

			foreach (string ns in namespaces)
			{
				string name = String.Join(".", namespaces, 0, count + 1);
				Item i = lib.FindItem(name);

				// First look into our cache
				if (processed.ContainsKey(name))
				{
					parent = processed[name];
				}
				else
				{
					// We create or either a namespace or what the GenerateItem returns
					if (i == null)
					{
						// No parent, namespace for sure
						if (parent == null)
						{
							CodeNamespace cns = new CodeNamespace(name);
							cu.Namespaces.Add(cns);
							parent = cns;
							// Add it to the dict of processed
							processed[name] = cns;
						}
						// Namespace parent, new namespace but with the '.'
						// to mark a sub namespace
						else if (parent.GetType() == typeof(CodeNamespace))
						{
							CodeNamespace cns = new CodeNamespace(name);
							cu.Namespaces.Add(cns);
							parent = cns;
							// Add it to the dict of processed
							processed[name] = cns;
						}
						// We do support sub classes
						else
						{
							CodeTypeDeclaration ty = new CodeTypeDeclaration(ns);
							parent = ty;
							// Add it to the dict of processed
							processed[name] = ty;
						}
					}
					else
					{
						parent = GenerateItem(i);
					}
				}
				count++;				
			}
			return parent;
		}

		private string GenerateRetPinvoke(Arg arg)
		{
			if (arg == null)
				return "void";

			Item i = arg.ArgType;
			if (i == null)
			{
				Console.WriteLine("[WRN] Arg '" + arg.Name + "' without a type?");
				return "IntPtr";
			}
			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
					return null;
				// TODO simple cases
				case ItemType.BASIC:
					return null;
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					return "IntPtr";
				case ItemType.OBJECT:
					return "IntPtr";
				case ItemType.STRUCT:
					return "IntPtr";
				// TODO same as basic?
				case ItemType.CONSTANT:
					return null;
				// TODO Check the processed for the enum name
				case ItemType.ENUM:
					return null;
				case ItemType.DEF:
					return null;
				default:
					return null;
			}	
		}

		private string GenerateArgPinvoke(Arg arg)
		{
			if (arg == null)
				return null;

			string ret = null;
			Item i = arg.ArgType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + arg.Name + "' without a type?");
				ret = "IntPtr";
			}
			else
			{
				switch (i.Type)
				{
					// Impossible cases
					case ItemType.INVALID:
					case ItemType.ATTR:
					case ItemType.ARG:
						ret = null;
						break;
					// TODO simple cases
					case ItemType.BASIC:
						ret = null;
						break;
					// TODO how to handle a function ptr?
					case ItemType.FUNCTION:
						ret = "IntPtr";
						break;
					case ItemType.OBJECT:
						ret = "IntPtr";
						break;
					case ItemType.STRUCT:
						ret = "IntPtr";
						break;
					// TODO same as basic?
					case ItemType.CONSTANT:
						ret = null;
						break;
					// TODO Check the processed for the enum name
					case ItemType.ENUM:
						ret = null;
						break;
					case ItemType.DEF:
						ret = null;
						break;
					default:
						ret = null;
						break;
				}
			}
			ret += " " + arg.Name;
			return ret;
		}

		private string GenerateNamePinvoke(Function f)
		{
			Item parent = f.Parent;
			if (parent == null)
			{
				// TODO use the correct replacement to support case/notation
				return f.Name.Replace(".", "_");
			}
			else
			{
				// TODO use the correct replacement to support case/notation
				string fName = parent.Namespace.Replace(".", "_") + "_" + parent.Identifier + "_" + f.Identifier;
				return fName;
			}
		}

		private string GenerateArgsPinvoke(Function f)
		{
			string ret;
			List args = f.Args;

			if (args == null)
				return null;

			string[] argsString = new string[args.Count];
			// Generate each arg string
			for (uint i = 0; i < args.Count; i++)
			{
				argsString[i] = GenerateArgPinvoke((Arg)args.Nth(i));
			}
			ret = String.Join(", ", argsString);
			return ret;
		}

		private CodeSnippetTypeMember GeneratePinvoke(Function f)
		{
			string pinvoke = string.Format("[DllImport(\"{0}.dll\") CallingConvention=CallingConvention.Cdecl]", lib.Name);
			// Handle the return value
			string retString = GenerateRetPinvoke(f.Ret);
			// Handle the function name
			string fName = GenerateNamePinvoke(f);
			// Handle the args
			string argsString = GenerateArgsPinvoke(f);
			pinvoke += string.Format("\nprivate static extern {0} {1}({2});", retString, fName, argsString);
			CodeSnippetTypeMember ext = new CodeSnippetTypeMember(pinvoke);
			return ext;
		}

		private CodeTypeDeclaration GenerateObject(Object o)
		{
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(o.Identifier);
			// TODO make the object disposable
			// Get the constructors
			List ctors = o.Ctors;
			if (ctors != null)
			{
				foreach (Function f in ctors)
				{
					co.Members.Add(GeneratePinvoke(f));
					CodeConstructor cc = new CodeConstructor();
					cc.Attributes = MemberAttributes.Public;
					co.Members.Add(cc);
				}
			}
#if FOO
			// add the base type
			IntPtr inherit = ender_item_object_inherit_get(item);
			if (inherit != IntPtr.Zero)
			{
				o.BaseTypes.Add(getItemName(inherit));
				ender_item_unref(inherit);
			}
#endif
			return co;
		}

		private void GenerateLib()
		{
			CodeNamespace root = new CodeNamespace();
			cu.Namespaces.Add(root);
			// Our default imports
			root.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
		}

		private CodeObject GenerateItem(Item item)
		{
			CodeObject ret = null;
			CodeObject parent;

			// check if the item has been already processed
			if (processed.ContainsKey(item.Name))
				return processed[item.Name];

			// generate all the parent classes/namespaces
			parent = GenerateParentObjects(item);
			if (parent == null)
			{
				Console.WriteLine("[ERR] Impossible to generate parent for '" + item.Name + "'");
				return null;
			}

			Console.WriteLine("Generating item '" + item.Name + "'");
			// Finally generate the particular item
			switch (item.Type)
			{
				case ItemType.OBJECT:
					ret = GenerateObject((Object)item);
					break;
				default:
					break;
			}
			if (ret == null)
			{
				Console.WriteLine("[ERR] Impossible to generate type '" + item.Name + "'");
				return ret;
			}

			// Add the generated type into our hash
			processed[item.Name] = ret;

			// Add it to the parent object
			if (parent.GetType() == typeof(CodeNamespace))
			{
				// We can not add namespaces into another namespace
				if (ret.GetType() == typeof(CodeNamespace))
					cu.Namespaces.Add((CodeNamespace)ret);
				else
				{
					CodeNamespace ns = (CodeNamespace)parent;
					ns.Types.Add((CodeTypeDeclaration)ret);
				}
			}
			else
			{
				CodeTypeDeclaration ty = (CodeTypeDeclaration)parent;
				ty.Members.Add((CodeTypeDeclaration)ret);
			}
			return ret;
		}
		public CodeCompileUnit Generate()
		{
			if (lib == null)
				return cu;

			// First set the information from the library itself
			GenerateLib();
			// Iterate over every type and generate it
			foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
			{
				List items = lib.List(type);
				foreach (Item item in items)
				{
					GenerateItem(item);
				}
			}
			return cu;
		}
	}
}
