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

		private CodeTypeDeclaration GenerateObject(Object o)
		{
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(o.Identifier);
			// Get the constructors
			List ctors = o.Ctors;
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
