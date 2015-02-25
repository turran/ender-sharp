using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace Ender
{
	public class Generator
	{
		private CodeCompileUnit cu;
		private CodeNamespace root;
		private Lib lib;
		private Dictionary<string, CodeObject> processed;
		private CodeDomProvider provider;

		private Generator()
		{
		}

		public Generator(string name, CodeDomProvider provider)
		{
			// Create our empty compilation unit
			cu = new CodeCompileUnit();
			// Find the ender library
			lib = Lib.Find(name);
			// Our dictionary to keep processed items in sync
 			processed = new Dictionary<string, CodeObject>();
			// Our code provider to identify keywords
			this.provider = provider;
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
				// Basic cases
				case ItemType.BASIC:
					Basic b = (Basic)i;
					// The special case for const char *
					if (b.ValueType == ValueType.STRING && arg.Transfer == ItemTransfer.NONE)
						return "IntPtr";
					else
						return GenerateBasicPinvoke((Basic)i);
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

		private string GenerateBasicPinvoke(Basic b)
		{
			switch (b.ValueType)
			{
				case ValueType.BOOL:
					return "bool";
				case ValueType.UINT8:
					return "byte";
				case ValueType.INT8:
					return "sbyte";
				case ValueType.UINT32:
					return "uint";
				case ValueType.INT32:
					return "int";
				case ValueType.UINT64:
					return "ulong";
				case ValueType.INT64:
					return "long";
				case ValueType.DOUBLE:
					return "double";
				case ValueType.STRING:
					return "string";
				case ValueType.POINTER:
					return "IntPtr";
				case ValueType.SIZE:
					return "IntPtr";
				default:
					return "object";
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
					// Basic case
					case ItemType.BASIC:
						Basic b = (Basic)i;
						// The special case for in, full char *
						if (b.ValueType == ValueType.STRING &&
								arg.Transfer == ItemTransfer.FULL
								&& arg.Direction == ArgDirection.IN)
							ret = "IntPtr";
						else
							ret = GenerateBasicPinvoke((Basic)i);
						break;
					// TODO how to handle a function ptr?
					case ItemType.FUNCTION:
						ret = "IntPtr";
						break;
					case ItemType.OBJECT:
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
			if (arg.Direction == ArgDirection.OUT)
				ret = "out " + ret;
			ret += " " + provider.CreateValidIdentifier(arg.Name);
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

		private void GenerateDisposable(CodeTypeDeclaration co, Function unrefFunc)
		{
			if (provider.Supports(GeneratorSupport.DeclareInterfaces))
			{
				// make the object disposable
				co.BaseTypes.Add("IDisposable");
				// Add the overloaded functions
				CodeMemberField disposedField = new CodeMemberField(typeof(bool), "disposed");
				co.Members.Add(disposedField);
				// Add the dispose method
				CodeMemberMethod dispose1 = new CodeMemberMethod();
				dispose1.Name = "Dispose";
				dispose1.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(null, "Dispose", new CodePrimitiveExpression(false))));
				dispose1.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("GC"), "SuppressFinalize", new CodeThisReferenceExpression())));
				co.Members.Add(dispose1);
				// Add the dispose protected method
				CodeMemberMethod dispose2 = new CodeMemberMethod();
				dispose2.Name = "Dispose";
				dispose2.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "disposing"));
				dispose2.Attributes = MemberAttributes.Family;
				CodeStatement cs = new CodeConditionStatement(new CodeVariableReferenceExpression("disposed"),
						new CodeStatement[] {},
						new CodeStatement[] {
							// unref(raw)
							new CodeExpressionStatement(new CodeMethodInvokeExpression(null, GenerateNamePinvoke(unrefFunc), new CodeVariableReferenceExpression("raw"))),
							// raw = IntPtr.Zero
							new CodeAssignStatement(new CodeVariableReferenceExpression("raw"), new CodeTypeReferenceExpression("IntPtr.Zero")),
							// disposed = false
							new CodeAssignStatement(new CodeVariableReferenceExpression("disposed"), new CodePrimitiveExpression(false))
						});
				dispose2.Statements.Add(cs);
				co.Members.Add(dispose2);
				// Add the dstructor that will call Dispose(false), seems that CodeDom does not support destructors!
				CodeSnippetTypeMember ext = new CodeSnippetTypeMember("~" + co.Name + "() { Dispose(false); }");
				co.Members.Add(ext);
			}
			else
			{
				// make the destructor call directly the unref
				CodeSnippetTypeMember ext = new CodeSnippetTypeMember("~" + co.Name + "() { " + GenerateNamePinvoke(unrefFunc) + "(); }");
				co.Members.Add(ext);
			}
		}

		private CodeSnippetTypeMember GeneratePinvoke(Function f)
		{
			string pinvoke = string.Format("[DllImport(\"{0}.dll\", CallingConvention=CallingConvention.Cdecl)]", lib.Name);
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

		private CodeTypeReference GenerateBasic(Basic b)
		{
			System.Type st;
			switch (b.ValueType)
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

		private CodeParameterDeclarationExpression GenerateArg(Arg arg)
		{
			CodeParameterDeclarationExpression ret = null;
			Item i = arg.ArgType;

			if (i == null)
				return null;
			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
					ret = null;
					break;
				// Basic case
				case ItemType.BASIC:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = GenerateBasic((Basic)i);
					break;
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					ret = null;
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(i.Identifier);
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

			if (ret == null)
				return ret;
			if (arg.Direction == ArgDirection.OUT)
				ret.Direction = FieldDirection.Out;
			ret.Name = provider.CreateValidIdentifier(arg.Name);
			return ret;
		}

		private CodeTypeReference GenerateRet(Arg arg)
		{
			if (arg == null)
				return null;

			Item i = arg.ArgType;
			if (i == null)
				return null;

			CodeTypeReference ret = null;
			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
					ret = null;
					break;
				// Basic case
				case ItemType.BASIC:
					ret = GenerateBasic((Basic)i);
					break;
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					ret = null;
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeTypeReference(i.Identifier);
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
			return ret;
		}

		private CodeMemberMethod GenerateFunction(Function f)
		{
			CodeMemberMethod cm = null;
			// We for sure invoke a function
			CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
			ci.Method = new CodeMethodReferenceExpression(null, GenerateNamePinvoke(f));
			// The default statement will be the invoke, unless we have a return value
			CodeStatement cs = new CodeExpressionStatement(ci);
			// To know if we must skip the first arg
			bool skipFirst = false;

			if ((f.Flags & FunctionFlag.CTOR) == FunctionFlag.CTOR)
			{
				cm = new CodeConstructor();
				cm.Attributes = MemberAttributes.Public;
			}
			else if ((f.Flags & FunctionFlag.IS_METHOD) == FunctionFlag.IS_METHOD)
			{
				cm = new CodeMemberMethod();
				cm.Name = f.Identifier;
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				// Add the raw
				ci.Parameters.Add(new CodeVariableReferenceExpression("raw"));
				skipFirst = true;
			}

			// Now the args
			List args = f.Args;
			if (args != null)
			{
				// If it is a method, skip the first Arg, it will be self
				int count = 0;
				foreach (Arg a in args)
				{
					if (count == 0 && skipFirst)
					{
						count++;
						continue;
					}

					CodeParameterDeclarationExpression cp = GenerateArg(a);
					if (cp != null)
					{
						// TODO Add any conversion we might need
						// Add the invoke function
						// Add the parameter
						ci.Parameters.Add(new CodeVariableReferenceExpression(cp.Name));
						cm.Parameters.Add(cp);
					}
				}
			}
			// Now the return value
			if ((f.Flags & FunctionFlag.CTOR) != FunctionFlag.CTOR)
			{
				CodeTypeReference ret = GenerateRet(f.Ret);
				if (ret != null)
				{
					// Check if we need to generate more statements to transform the return value
					cs = new CodeMethodReturnStatement(ci);
					if (cm != null)
						cm.ReturnType = ret;
				}
			}


			// The body
			if (cm != null)
				cm.Statements.Add(cs);

			return cm;
		}

		private CodeTypeDeclaration GenerateEnum(Enum e)
		{
			Console.WriteLine("Generating enum " + e.Name);
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(e.Identifier);
			co.IsEnum = true;
			return co;
		}

		private CodeTypeDeclaration GenerateStruct(Struct s)
		{
			Console.WriteLine("Generating struct " + s.Name);
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(s.Identifier);
			// Generate the raw field
			CodeMemberField rawField = new CodeMemberField("IntPtr", "raw");
			rawField.Attributes = MemberAttributes.Family;
			co.Members.Add(rawField);
			return co;
		}

		private CodeTypeDeclaration GenerateObject(Object o)
		{
			Function refFunc = null;
			Function unrefFunc = null;
			List functions;
			bool hasRef = false;
			bool hasUnref = false;

			Console.WriteLine("Generating object " + o.Name);
			// Do nothing if cannot ref an object
			Object tmp = o;
			while (tmp != null && !hasRef && !hasUnref)
			{
				functions = tmp.Functions;
				if (functions != null)
				{
					foreach (Function f in functions)
					{
						if ((f.Flags & FunctionFlag.REF) == FunctionFlag.REF)
						{
							refFunc = f;
							hasRef = true;
						}
						if ((f.Flags & FunctionFlag.UNREF) == FunctionFlag.UNREF)
						{
							unrefFunc = f;
							hasUnref = true;
						}
					}
				}
				tmp = tmp.Inherit;
			}
			if (!hasRef || !hasUnref)
			{
				Console.WriteLine("[ERR] Skipping object " + o.Name + ", it does not have a ref/unref function");
				return null;
			}

			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(o.Identifier);
			// In case it does no inherit from anything, add the raw pointer
			Object inherit = o.Inherit;
			if (inherit == null)
			{
				CodeMemberField rawField = new CodeMemberField("IntPtr", "raw");
				rawField.Attributes = MemberAttributes.Family;
				co.Members.Add(rawField);
				GenerateDisposable(co, unrefFunc);
			}
			else
			{
				// add the inheritance on the type
				if (!processed.ContainsKey(inherit.Name))
					GenerateItem(inherit);
				if (processed.ContainsKey(inherit.Name))
				{
					CodeTypeDeclaration cob = (CodeTypeDeclaration)processed[inherit.Name];
					co.BaseTypes.Add(cob.Name);
				}	
			}
			// Create every pinvoke
			functions = o.Functions;
			if (functions != null)
			{
				foreach (Function f in functions)
				{
					Console.WriteLine("Processing function " + f.Name);
					co.Members.Add(GeneratePinvoke(f));
				}

			}
			// Get the constructors
			List ctors = o.Ctors;
			if (ctors != null)
			{
				// Create a dummy constructor to handle the owning of a pointer
				CodeConstructor cc = new CodeConstructor();
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "i"));
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
				cc.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("i"));
				cc.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("owned"));
				// must be protected
				cc.Attributes = MemberAttributes.Family;
				co.Members.Add(cc);
			}
			else
			{
				// Make the prvate constructor
				CodeConstructor cc = new CodeConstructor();
				cc.Attributes = MemberAttributes.Private;
				co.Members.Add(cc);

				// Make the protected constructor
				cc = new CodeConstructor();
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "i"));
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
				CodeAssignStatement as1 = new CodeAssignStatement(new CodeVariableReferenceExpression("raw"),
						new CodeVariableReferenceExpression("i"));
				cc.Statements.Add(as1);
				CodeMethodInvokeExpression invoke1 = new CodeMethodInvokeExpression(
						null,
						GenerateNamePinvoke(refFunc),
						new CodeVariableReferenceExpression("i"));
				CodeConditionStatement cs = new CodeConditionStatement(new CodeVariableReferenceExpression("owned"),
						new CodeExpressionStatement(invoke1));
				cc.Statements.Add(cs);
				// FIXME must be protected
				cc.Attributes = MemberAttributes.Public;
				co.Members.Add(cc);
			}
			// in case the object has a ref() method
			foreach (Function f in o.Functions)
			{
				// Skip the ref/unref
				if ((f.Flags & FunctionFlag.REF) == FunctionFlag.REF)
					continue;
				if ((f.Flags & FunctionFlag.UNREF) == FunctionFlag.UNREF)
					continue;

				Console.WriteLine("Generating function " + f.Name);
				CodeMemberMethod cm = GenerateFunction(f);
				if (cm != null)
					co.Members.Add(cm);
			}

			return co;
		}

		private void GenerateLib()
		{
			CodeNamespace root = new CodeNamespace();
			cu.Namespaces.Add(root);
			// Our default imports
			root.Imports.Add(new CodeNamespaceImport("System"));
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
				case ItemType.STRUCT:
					ret = GenerateStruct((Struct)item);
					break;
				case ItemType.ENUM:
					ret = GenerateEnum((Enum)item);
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
			foreach (ItemType type in System.Enum.GetValues(typeof(ItemType)))
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
