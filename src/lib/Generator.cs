using System;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Generator
	{
		private CodeCompileUnit cu;
		private CodeNamespace root;
		private Lib lib;
		private Dictionary<string, CodeObject> processed;
		private CodeDomProvider provider;

		public Generator(Lib lib, CodeDomProvider provider)
		{
			this.lib = lib;
			// Create our empty compilation unit
			cu = new CodeCompileUnit();
			// Our dictionary to keep processed items in sync
 			processed = new Dictionary<string, CodeObject>();
			// Our code provider to identify keywords
			this.provider = provider;
		}

		private string ConvertFullName(string name)
		{
			string[] values = name.Split('.');
			string[] retValues = new string[values.Length];

			for (int i = 0; i < values.Length; i++)
			{
				retValues[i] = Utils.Convert(values[i], Utils.Case.UNDERSCORE,
						lib.Notation, Utils.Case.PASCAL, Utils.Notation.ENGLISH);
			}
			return String.Join(".", retValues);
		}

		private string ConvertName(string id)
		{
			return Utils.Convert(id, Utils.Case.UNDERSCORE, lib.Notation,
					Utils.Case.PASCAL, Utils.Notation.ENGLISH);
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
					// We create or either a namespace or what the GenerateComplexItem returns
					if (i == null)
					{
						// No parent, namespace for sure
						if (parent == null)
						{
							CodeNamespace cns = new CodeNamespace(ConvertFullName(name));
							cu.Namespaces.Add(cns);
							parent = cns;
							// Add it to the dict of processed
							processed[name] = cns;
						}
						// Namespace parent, new namespace but with the '.'
						// to mark a sub namespace
						else if (parent.GetType() == typeof(CodeNamespace))
						{
							CodeNamespace cns = new CodeNamespace(ConvertFullName(name));
							cu.Namespaces.Add(cns);
							parent = cns;
							// Add it to the dict of processed
							processed[name] = cns;
						}
						// We do support sub classes
						else
						{
							CodeTypeDeclaration ty = new CodeTypeDeclaration(ConvertName(ns));
							parent = ty;
							// Add it to the dict of processed
							processed[name] = ty;
						}
					}
					else
					{
						parent = GenerateComplexItem(i);
					}
				}
				count++;				
			}
			return parent;
		}

		private string GenerateRetPinvokeFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
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
					if (b.ValueType == ValueType.STRING && transfer == ItemTransfer.NONE)
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
					return "IntPtr";
				case ItemType.ENUM:
					// if the created object is actually an enum (it can be a class in
					// case it has methods) use it, otherwise the inner enum
					CodeTypeDeclaration ce = (CodeTypeDeclaration)GenerateComplexItem(i);
					if (ce.IsEnum)
					{
						return ConvertFullName(i.Name) + "Enum";
					}
					else
					{
						return ConvertFullName(i.Name) + "Enum" + ".Enum";
					}
				case ItemType.DEF:
					Def def = (Def)i;
					return GenerateRetPinvokeFull(def.DefType, name, direction, transfer);
				default:
					return null;
			}	
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

			return GenerateRetPinvokeFull(i, arg.Name, arg.Direction, arg.Transfer);
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

		private string GenerateArgPinvokeFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			string ret = null;

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
							transfer == ItemTransfer.FULL
							&& direction == ArgDirection.IN)
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
				case ItemType.ENUM:
					// if the created object is actually an enum (it can be a class in
					// case it has methods) use it, otherwise the inner enum
					CodeTypeDeclaration ce = (CodeTypeDeclaration)GenerateComplexItem(i);
					if (ce.IsEnum)
					{
						ret = ConvertFullName(i.Name) + "Enum";
					}
					else
					{
						ret = ConvertFullName(i.Name) + "Enum" + ".Enum";
					}
					break;
				case ItemType.DEF:
					Def d = (Def)i;
					return GenerateArgPinvokeFull(d.DefType, name, direction, transfer);
				default:
					ret = null;
					break;
			}
			// For structs, the out is irrelevant
			if (direction == ArgDirection.OUT && i.Type != ItemType.STRUCT)
				ret = "out " + ret;
			ret += " " + provider.CreateValidIdentifier(name);
			return ret;
		}

		private string GenerateArgPinvoke(Arg arg)
		{
			if (arg == null)
				return null;

			Item i = arg.ArgType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + arg.Name + "' without a type?");
				return "IntPtr " + arg.Name;
			}
			return GenerateArgPinvokeFull(i, arg.Name, arg.Direction, arg.Transfer);
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
				string fName;
				// TODO use the correct replacement to support case/notation
				// in case the parent is an attribute, we will have another parent
				if (parent.Type == ItemType.ATTR)
				{
					fName = parent.Parent.Name.Replace(".", "_") + "_" + parent.Name + "_" + f.Identifier;
				}
				else
				{
					fName = parent.Namespace.Replace(".", "_") + "_" + parent.Identifier + "_" + f.Identifier;
				}
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
				dispose1.Attributes = MemberAttributes.Public;
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

		private CodeTypeReference GenerateType(Item i)
		{
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
					ret = new CodeTypeReference(ConvertFullName(i.Name));
					break;
				// TODO same as basic?
				case ItemType.CONSTANT:
					ret = null;
					break;
				case ItemType.ENUM:
					// if the created object is actually an enum (it can be a class in
					// case it has methods) use it, otherwise the inner enum
					CodeTypeDeclaration ce = (CodeTypeDeclaration)GenerateComplexItem(i);
					if (ce.IsEnum)
					{
						ret = new CodeTypeReference(ConvertFullName(i.Name) + "Enum");
					}
					else
					{
						ret = new CodeTypeReference(ConvertFullName(i.Name) + "Enum" + ".Enum");
					}
					break;
				case ItemType.DEF:
					Def def = (Def)i;
					ret = GenerateType(def.DefType);
					break;
				default:
					ret = null;
					break;
			}
			return ret;
		}

		private CodeTypeReference GenerateProp(Attr attr)
		{
			if (attr == null)
				return null;

			Item i = attr.AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + attr.Name + "' without a type?");
				return new CodeTypeReference("System.IntPtr");
			}

			return GenerateType(i);
		}


		// The statements needed to convert an arg from Pinvoke to C#
		private CodeStatement GenerateArgPostStatementFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			switch (i.Type)
			{
				case ItemType.OBJECT:
					// For out objects, create a new object based on the raw
					if (direction == ArgDirection.OUT)
						return new CodeAssignStatement(new CodeVariableReferenceExpression(name), new CodeObjectCreateExpression(
							new CodeTypeReference(ConvertFullName(i.Name)), new CodeExpression[] {
							new CodeVariableReferenceExpression(name + "Raw"),
							new CodePrimitiveExpression(false)
							}));
					return null;
				default:
					return null;
			}
		}

		// The statements needed to convert an arg from Pinvoke to C#
		private CodeStatement GenerateArgPostStatement(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
				return null;
			return GenerateArgPostStatementFull(i, arg.Name, arg.Direction, arg.Transfer);
		}

		// The statements needed to convert an arg from C# to Pinvoke
		private CodeStatement GenerateArgPreStatementFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			switch (i.Type)
			{
				case ItemType.DEF:
					Def d = (Def)i;
					return GenerateArgPreStatementFull(d.DefType, name, direction, transfer);
				case ItemType.STRUCT:
					// For out structs, we need to create the struct first before passing the raw arg
					if (direction == ArgDirection.OUT)
						return new CodeAssignStatement(new CodeVariableReferenceExpression(name), new CodeObjectCreateExpression(
							new CodeTypeReference(ConvertFullName(i.Name))));
					return null;
				case ItemType.OBJECT:
					// For out objects, just pass an IntPtr
					if (direction == ArgDirection.OUT)
						return new CodeVariableDeclarationStatement(typeof(IntPtr), name + "Raw");
					return null;
				default:
					return null;
			}
		}

		// The statements needed to convert an arg from C# to Pinvoke
		private CodeStatement GenerateArgPreStatement(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
				return null;
			return GenerateArgPreStatementFull(i, arg.Name, arg.Direction, arg.Transfer);
		}

		// The expression to pass into the Pinvoke method for this arg
		private CodeExpression GenerateArgExpressionFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			CodeExpression ret = null;
			CodeVariableReferenceExpression cvr;

			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
					break;
				// call itself with the def pointer
				case ItemType.DEF:
					Def d = (Def)i;
					return GenerateArgExpressionFull(d.DefType, name, direction, transfer);
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
				// TODO same as basic?
				case ItemType.CONSTANT:
				// TODO Check the processed for the enum name
				case ItemType.ENUM:
				case ItemType.BASIC:
					cvr = new CodeVariableReferenceExpression();
					cvr.VariableName = provider.CreateValidIdentifier(name);
					ret = cvr;
					break;
				case ItemType.STRUCT:
					cvr = new CodeVariableReferenceExpression();
					cvr.VariableName = provider.CreateValidIdentifier(name);
					ret = new CodePropertyReferenceExpression(cvr, "Raw");
					break;
				case ItemType.OBJECT:
					if (direction == ArgDirection.OUT)
					{
						ret = new CodeVariableReferenceExpression(name + "Raw");

					}
					else
					{
						cvr = new CodeVariableReferenceExpression();
						cvr.VariableName = provider.CreateValidIdentifier(name);
						ret = new CodePropertyReferenceExpression(cvr, "Raw");
					}
					break;
				default:
					break;
			}

			if (ret == null)
				return ret;

			if (direction == ArgDirection.OUT && i.Type != ItemType.STRUCT)
				return new CodeDirectionExpression(FieldDirection.Out, ret);
			else
				return ret;
		}

		// The expression to pass into the Pinvoke method for this arg
		private CodeExpression GenerateArgExpression(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
			{
				Console.WriteLine("[ERR] Invalid arg " + arg.Name);
				CodeVariableReferenceExpression cvr = new CodeVariableReferenceExpression();
				cvr.VariableName = provider.CreateValidIdentifier(arg.Name);
				return cvr;
			}
			return GenerateArgExpressionFull(i, arg.Name, arg.Direction, arg.Transfer);
		}

		private CodeParameterDeclarationExpression GenerateArgFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			CodeParameterDeclarationExpression ret = null;
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
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference("System.IntPtr");
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(ConvertFullName(i.Name));
					break;
				// TODO same as basic?
				case ItemType.CONSTANT:
					ret = null;
					break;
				case ItemType.ENUM:
					// if the created object is actually an enum (it can be a class in
					// case it has methods) use it, otherwise the inner enum
					CodeTypeDeclaration ce = (CodeTypeDeclaration)GenerateComplexItem(i);
					if (ce.IsEnum)
					{
						ret = new CodeParameterDeclarationExpression();
						ret.Type = new CodeTypeReference(ConvertFullName(i.Name) + "Enum");
					}
					else
					{
						ret = new CodeParameterDeclarationExpression();
						ret.Type = new CodeTypeReference(ConvertFullName(i.Name) + "Enum" + ".Enum");
					}
					break;
				case ItemType.DEF:
					Def d = (Def)i;
					return GenerateArgFull(d.DefType, name, direction, transfer);
				default:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference("System.IntPtr");
					break;
			}

			if (ret == null)
				return ret;
			if (direction == ArgDirection.OUT)
				ret.Direction = FieldDirection.Out;
			ret.Name = provider.CreateValidIdentifier(name);
			return ret;

		}

		private CodeParameterDeclarationExpression GenerateArg(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + arg.Name + "' without a type?");
				CodeParameterDeclarationExpression ret = new CodeParameterDeclarationExpression();
				ret.Type = new CodeTypeReference("System.IntPtr");
				ret.Name = provider.CreateValidIdentifier(arg.Name);
				return ret;
			}
			return GenerateArgFull(i, arg.Name, arg.Direction, arg.Transfer);
		}

		private CodeStatement GenerateRetStatement(Arg arg)
		{
			if (arg == null)
				return null;

			Item i = arg.ArgType;
			if (i == null)
				return null;

			CodeStatement ret = null;
			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
					ret = null;
					break;
				case ItemType.BASIC:
					Basic b = (Basic)i;
					// The special case for const char *: Marshal.PtrToStringAnsi(ret)
					if (b.ValueType == ValueType.STRING && arg.Transfer == ItemTransfer.NONE)
						ret = new CodeMethodReturnStatement(
								new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Marshal"),
								"PtrToStringAnsi", new CodeVariableReferenceExpression("ret")));
					else
						ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("ret"));
					break;
				case ItemType.ENUM:
					ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("ret"));
					break;
				case ItemType.OBJECT:
					// Call the constructor
					// return new identifier(ret, false/true);
					ret = new CodeMethodReturnStatement(new CodeObjectCreateExpression(
							new CodeTypeReference(ConvertName(i.Identifier)),
							new CodeVariableReferenceExpression("ret"),
							new CodePrimitiveExpression(false)));
					break;
				default:
					ret = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("ret"));
					break;
			}
			return ret;
		}

		private CodeTypeReference GenerateRet(Arg arg)
		{
			if (arg == null)
				return null;

			Item i = arg.ArgType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + arg.Name + "' without a type?");
				return new CodeTypeReference("System.IntPtr");
			}

			return GenerateType(i);
		}

		//    int ret;
		//    ret = my_method(i1, s2);
		//    return ret;
		private CodeStatementCollection GenerateFunctionBody(Function f)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			// To know if we must skip the first arg
			bool skipFirst = false;

			if ((f.Flags & FunctionFlag.IS_METHOD) == FunctionFlag.IS_METHOD)
				skipFirst = true;

			List args = f.Args;
			// Now the pre return statements
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

					// Add any pre statement we might need
					CodeStatement cs = GenerateArgPreStatement(a);
					if (cs != null)
						csc.Add(cs);
				}
			}

			// We for sure call the pinvoke function
			CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
			ci.Method = new CodeMethodReferenceExpression(null, GenerateNamePinvoke(f));

			if (skipFirst)
			{
				// Add the raw
				ci.Parameters.Add(new CodeVariableReferenceExpression("raw"));
			}

			// Now generate the pinvoke args
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

					// Add the expression to the invoke function
					CodeExpression ce = GenerateArgExpression(a);
					if (ce != null)
						ci.Parameters.Add(ce);
				}
			}

			// Now the return value prototype
			CodeTypeReference ret = GenerateRet(f.Ret);
			if (ret != null)
			{
				CodeVariableDeclarationStatement cvs;
				// Add the return value
				string sret = GenerateRetPinvoke(f.Ret);
				System.Type type = GenerateType(sret);
				if (type != null)
				{
					cvs = new CodeVariableDeclarationStatement(type, "ret", ci);
				}
				else
				{
					cvs = new CodeVariableDeclarationStatement(sret, "ret", ci);
				}
				csc.Add(cvs);
			}
			else
			{
				// Just call the method
				CodeStatement cs = new CodeExpressionStatement(ci);
				csc.Add(cs);
			}

			// Now the post statements
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

					// Add any pre statement we might need
					CodeStatement cs = GenerateArgPostStatement(a);
					if (cs != null)
						csc.Add(cs);
				}
			}

			// Finally the return value
			if (ret != null)
			{
				if ((f.Flags & FunctionFlag.CTOR) != FunctionFlag.CTOR)
				{
					CodeStatement cs = GenerateRetStatement(f.Ret);
					if (cs != null)
						csc.Add(cs);
				}
				// Initialize the raw type for ctors
				else
				{
					ci = new CodeMethodInvokeExpression();
					ci.Method = new CodeMethodReferenceExpression(null, "Initialize");
					ci.Parameters.Add(new CodeVariableReferenceExpression("ret"));
					ci.Parameters.Add(new CodePrimitiveExpression(false));
					CodeStatement cs = new CodeExpressionStatement(ci);
					csc.Add(cs);
				}
			}

			return csc;
		}

		// creates the member method in the form of:
		//  public int MyMethod(int i1, string s2)
		//  {
		//    int ret;
		//    ret = my_method(i1, s2);
		//    return ret;
		//
		private CodeMemberMethod GenerateFunction(Function f)
		{
			CodeMemberMethod cm = null;
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
				cm.Name = ConvertName(f.Identifier);
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				skipFirst = true;
			}
			else
			{
				cm = new CodeMemberMethod();
				cm.Name = ConvertName(f.Identifier);
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
			}

			// Now the args of the method itself
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

					// Add the method arg
					CodeParameterDeclarationExpression cp = GenerateArg(a);
					if (cp != null)
					{
						// Add the parameter
						if (cm != null)
							cm.Parameters.Add(cp);
					}
				}
			}

			// Now the return value prototype
			CodeTypeReference ret = GenerateRet(f.Ret);
			if (ret != null)
			{
				// Do not set a return value on ctors
				if ((f.Flags & FunctionFlag.CTOR) != FunctionFlag.CTOR)
				{
					if (cm != null)
					{
						cm.ReturnType = ret;
					}
				}
			}

			cm.Statements.AddRange(GenerateFunctionBody(f));

			return cm;
		}

		private CodeTypeDeclaration GenerateEnum(Enum e)
		{
			Console.WriteLine("Generating enum " + e.Name);
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(ConvertName(e.Identifier) + "Enum");
			// Get the values
			List values = e.Values;
			if (values != null)
			{
				foreach (Constant c in values)
				{
					CodeMemberField f = new CodeMemberField ();
					f.Name = ConvertName(c.Identifier);
					// TODO add the value
					//  InitExpression = new CodePrimitiveExpression(enumNameAndValue.Key);
         				co.Members.Add(f);
				}
			}
			// Add the generated type into our hash
			processed[e.Name] = co;
			co.IsEnum = true;
			return co;
		}

		private CodeMemberField GenerateInnerFieldFull(Item i, string iName, string name)
		{
			CodeMemberField ret = null;
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
					ret = new CodeMemberField();
					ret.Type = GenerateBasic((Basic)i);
					ret.Name = name;
					ret.Attributes = MemberAttributes.Public;
					break;
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					ret = null;
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeMemberField();
					ret.Type = new CodeTypeReference(ConvertFullName(iName));
					ret.Name = name;
					ret.Attributes = MemberAttributes.Public;
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
					Def def = (Def)i;
					ret = GenerateInnerFieldFull(def.DefType, iName, name);
					break;
				default:
					break;
			}
			return ret;
		}

		private CodeMemberField GenerateInnerField(Attr a)
		{
			Item i = a.AttrType;
			if (i == null)
			{
				Console.WriteLine("[ERR] Field '" + a.Name + "' without type");
				return null;
			}
			return GenerateInnerFieldFull(i, i.Name, a.Name);
		}

		// TODO The struct should have every field on the inner
		// struct repeated. And a constructor based on a raw and a constructor
		// generic. In case of an out param being a struct, allocate the
		// unmanaged memory, call the function and then set the class members
		// Another option is at is right now, always have an unmanaged memory
		// and use that
		private CodeTypeDeclaration GenerateStruct(Struct s)
		{
			Console.WriteLine("Generating struct " + s.Name);
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(ConvertName(s.Identifier));
			// Add the generated type into our hash
			processed[s.Name] = co;
			// Generate the raw field
			CodeMemberField rawField = new CodeMemberField("IntPtr", "raw");
			rawField.Attributes = MemberAttributes.Family;
			co.Members.Add(rawField);
			// Add the getter
			CodeMemberProperty rawProp = new CodeMemberProperty();
			rawProp.Name = "Raw";
			rawProp.Type = new CodeTypeReference("System.IntPtr");
			rawProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			// Declares a property get statement to return the value of the raw IntPtr
			rawProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "raw")));
			co.Members.Add(rawProp);

			List fields = s.Fields;
			// TODO add the getters/setters
			// Add the inner struct
			CodeTypeDeclaration cs = new CodeTypeDeclaration(ConvertName(s.Identifier) + "Struct");
			cs.Attributes = MemberAttributes.Private;
			cs.IsStruct = true;
			// Add the custom attributes [StructLayout(LayoutKind.Sequential)]
			cs.CustomAttributes.Add(new CodeAttributeDeclaration("StructLayout",
					new CodeAttributeArgument(new CodeFieldReferenceExpression(
					new CodeTypeReferenceExpression(typeof(LayoutKind)), "Sequential"))));
			// Add the fields to the struct
			if (fields != null)
			{
				foreach (Attr f in fields)
				{
					CodeMemberField mf = GenerateInnerField(f);
					if (mf != null)
						cs.Members.Add(mf);
				}
			}
			co.Members.Add(cs);
			// Add the contructor which will allocate the raw pointer memory
			CodeConstructor cc = new CodeConstructor();
			cc.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			// raw = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(p)))
			CodeMethodInvokeExpression cms = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "SizeOf", new CodeTypeOfExpression(new CodeTypeReference(ConvertName(s.Identifier) + "Struct")));
			CodeMethodInvokeExpression cma = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "AllocHGlobal", cms);
			CodeAssignStatement cas = new CodeAssignStatement(new CodeVariableReferenceExpression("raw"), cma);
			cc.Statements.Add(cas);
			co.Members.Add(cc);
			// Make it disposable to remove the allocated memory
			return co;
		}

		private void GenerateIntantiableObject(Object o, CodeTypeDeclaration co, Function refFunc, Function unrefFunc)
		{
			// In case it does no inherit from anything:
			Object inherit = o.Inherit;

			if (inherit == null)
			{
				// Add the raw pointer: IntPtr raw;
				CodeMemberField rawField = new CodeMemberField("IntPtr", "raw");
				rawField.Attributes = MemberAttributes.Family;
				co.Members.Add(rawField);
				GenerateDisposable(co, unrefFunc);
				// Add the getter
				CodeMemberProperty rawProp = new CodeMemberProperty();
				rawProp.Name = "Raw";
				rawProp.Type = new CodeTypeReference("System.IntPtr");
				rawProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
				// Declares a property get statement to return the value of the raw IntPtr
				rawProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "raw")));
				co.Members.Add(rawProp);

				// add the initialize method
				CodeMemberMethod cm = new CodeMemberMethod();
				cm.Name = "Initialize";
				cm.Attributes = MemberAttributes.Family;
				cm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "i"));
				cm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
				// raw = i;
				CodeAssignStatement as1 = new CodeAssignStatement(new CodeVariableReferenceExpression("raw"),
						new CodeVariableReferenceExpression("i"));
				cm.Statements.Add(as1);
				// if (owned) ref(i)
				CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(null, GenerateNamePinvoke(refFunc));
				ci.Parameters.Add(new CodeVariableReferenceExpression("i"));
				CodeConditionStatement cs = new CodeConditionStatement(new CodeVariableReferenceExpression("owned"),
						new CodeExpressionStatement(ci));
				cm.Statements.Add(cs);
				co.Members.Add(cm);
			}
			else
			{
				// add the inheritance on the type
				if (!processed.ContainsKey(inherit.Name))
					GenerateComplexItem(inherit);
				if (processed.ContainsKey(inherit.Name))
				{
					CodeTypeDeclaration cob = (CodeTypeDeclaration)processed[inherit.Name];
					co.BaseTypes.Add(cob.Name);
				}
			}
			// Get the constructors
			List ctors = o.Ctors;
			if (ctors == null)
			{
				// Make the protected constructor
				CodeConstructor cc = new CodeConstructor();
				cc.Attributes = MemberAttributes.Family;
				co.Members.Add(cc);
			}

			// Create a dummy constructor to handle the owning of a pointer
			{
				CodeConstructor cc = new CodeConstructor();
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "i"));
				cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
				if (inherit != null)
				{
					cc.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("i"));
					cc.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression("owned"));
				}
				// must be protected
				cc.Attributes = MemberAttributes.FamilyOrAssembly;
				// Invoke the Initialize
				CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(null, "Initialize");
				ci.Parameters.Add(new CodeVariableReferenceExpression("i"));
				ci.Parameters.Add(new CodeVariableReferenceExpression("owned"));
				CodeStatement cs = new CodeExpressionStatement(ci);
				cc.Statements.Add(cs);
				co.Members.Add(cc);
			}

			// Create the properties
			List props = o.Props;
			if (props != null)
			{
				foreach (Attr a in props)
				{
					Function f;

					CodeMemberProperty cmp = new CodeMemberProperty();
					cmp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
					cmp.Name = ConvertName(a.Name);
					cmp.Type = GenerateProp(a);

					f = a.Getter;
					if (f != null)
					{
						cmp.GetStatements.AddRange(GenerateFunctionBody(f));
						cmp.HasGet = true;
					}
					f = a.Setter;
					if (f != null)
					{
						// Add the second parameter as a variable based on value
						// type arg1.Name
						Arg arg1 = (Arg)f.Args.Nth(1);
						cmp.SetStatements.Add(new CodeVariableDeclarationStatement(cmp.Type, arg1.Name));
						// arg1.Name = value
						cmp.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(arg1.Name), new CodePropertySetValueReferenceExpression()));
						cmp.SetStatements.AddRange(GenerateFunctionBody(f));
						cmp.HasSet = true;
					}
					co.Members.Add(cmp);
				}
			}
		}

		private CodeTypeDeclaration GenerateDef(Def d)
		{
			// For defs we can do:
			// for basic types, something like this:
			// <def name="enesim.argb" type="uint32">
			//
			// public class Color2 {
			// 	int color;
			// 	public Color2 (int color) {
			// 	this.color = color;
			// 	}
			// 	static public implicit operator Color2 (int color) {
			// 		return new Color2 (color);
			// 	}
			// 	static public implicit operator int (Color2 color) {
			// 		return color.color;
			// 	}
			// }
			// For complex types, we might inherit directly
			Item i = d.DefType;
			if (i == null)
				return null;

			CodeTypeDeclaration co;

			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
				case ItemType.FUNCTION:
				case ItemType.CONSTANT:
					return null;
				// Basic case
				case ItemType.BASIC:
 					co = new CodeTypeDeclaration(ConvertName(d.Identifier));
					CodeTypeReference ct = GenerateBasic((Basic)i);
					// Add the basic type as a Value member
					CodeMemberField valueField = new CodeMemberField(ct, "value");
					valueField.Attributes = MemberAttributes.Family;
					co.Members.Add(valueField);
					// Add the getter
					CodeMemberProperty valueProp = new CodeMemberProperty();
					valueProp.Name = "Value";
					valueProp.Type = GenerateBasic((Basic)i);
					valueProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
					// Declares a property get statement to return the value of the value IntPtr
					valueProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), "value")));
					co.Members.Add(valueProp);
					// Add the constructor
					CodeConstructor cc = new CodeConstructor();
					cc.Attributes = MemberAttributes.Public;
					cc.Parameters.Add(new CodeParameterDeclarationExpression(ct, "v"));
					cc.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("value"), new CodeVariableReferenceExpression("v")));
					co.Members.Add(cc);
					// Add the implicit operators
					// from its deftype to type
					CodeMemberMethod cmm = new CodeMemberMethod();
					cmm.Name = "implicit operator " + co.Name;
					cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
					cmm.ReturnType = new CodeTypeReference(" ");
					cmm.Parameters.Add(new CodeParameterDeclarationExpression(ct, "v"));
					cmm.Statements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(
							new CodeTypeReference(ConvertName(d.Identifier)),
							new CodeVariableReferenceExpression("v"))));
					co.Members.Add(cmm);
					// from type to deftype
					cmm = new CodeMemberMethod();
					cmm.Name = "implicit operator " + GenerateBasicPinvoke((Basic)i);
					cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
					cmm.ReturnType = new CodeTypeReference(" ");
					cmm.Parameters.Add(new CodeParameterDeclarationExpression(ConvertName(d.Identifier), "v"));
					cmm.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("v"), "value")));
					co.Members.Add(cmm);
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
				case ItemType.ENUM:
				case ItemType.DEF:
 					co = new CodeTypeDeclaration(ConvertName(d.Identifier));
					// add the inheritance on the type
					if (!processed.ContainsKey(i.Name))
						GenerateComplexItem(i);
					if (processed.ContainsKey(i.Name))
					{
						CodeTypeDeclaration cob = (CodeTypeDeclaration)processed[i.Name];
						co.BaseTypes.Add(cob.Name);
					}
					break;
				default:
					return null;
			}
			return co;
		}

		private CodeTypeDeclaration GenerateObject(Object o)
		{
			Function refFunc = null;
			Function unrefFunc = null;
			List functions;
			bool hasRef = false;
			bool hasUnref = false;
			bool hasCtor = false;
			bool hasMethod = false;
			bool isStaticClass = false;

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
						if ((f.Flags & FunctionFlag.CTOR) == FunctionFlag.CTOR)
						{
							hasCtor = true;
						}
						if ((f.Flags & FunctionFlag.IS_METHOD) == FunctionFlag.IS_METHOD)
						{
							hasMethod = true;
						}
					}
				}
				tmp = tmp.Inherit;
			}

			// A static class does not have constructors, methods or inherits from anything else
			if (!hasCtor && !hasMethod && o.Inherit == null)
				isStaticClass = true;

			// For objects that can be instantiables, if it does not have ref/unrefs, send an error
			if (!isStaticClass)
			{
				if (!hasRef || !hasUnref)
				{
					Console.WriteLine("[ERR] Skipping object " + o.Name + ", it does not have a ref/unref function");
					return null;
				}
			}

			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(ConvertName(o.Identifier));
			// Add the generated type into our hash
			processed[o.Name] = co;

			// Create every pinvoke for functions
			List funcs = o.Functions;
			if (funcs != null)
			{
				foreach (Function f in funcs)
				{
					Console.WriteLine("Processing PInvoke function " + f.Name);
					co.Members.Add(GeneratePinvoke(f));
				}
			}

			// Create every pinvoke for props
			List props = o.Props;
			if (props != null)
			{
				foreach (Attr a in props)
				{
					Function f;

					f = a.Getter;
					if (f != null)
					{
						Console.WriteLine("Processing Getter PInvoke function " + f.Name);
						co.Members.Add(GeneratePinvoke(f));
					}

					f = a.Setter;
					if (f != null)
					{
						Console.WriteLine("Processing Setter PInvoke function " + f.Name);
						co.Members.Add(GeneratePinvoke(f));
					}
				}
			}

			if (!isStaticClass)
				GenerateIntantiableObject(o, co, refFunc, unrefFunc);


			// in case the object has a ref() method
			if (funcs != null)
			{
				foreach (Function f in funcs)
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
			List deps = lib.Dependencies;
			if (deps != null)
			{
				foreach (Lib l in deps)
				{
					root.Imports.Add(new CodeNamespaceImport(ConvertName(l.Name)));
				}
			}
		}

		private CodeObject GenerateComplexItem(Item item)
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

			// It might be possible that generating parent objects creates the ourselves
			if (processed.ContainsKey(item.Name))
				return processed[item.Name];

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
				case ItemType.DEF:
					ret = GenerateDef((Def)item);
					break;
				default:
					break;
			}
			if (ret == null)
			{
				Console.WriteLine("[ERR] Impossible to generate type '" + item.Name + "'");
				return ret;
			}

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
			List items;
			if (lib == null)
				return cu;

			// First set the information from the library itself
			GenerateLib();
			// Generate very complex type (object, structs, enum and defs)
			ItemType[] complexTypes = { ItemType.OBJECT, ItemType.ENUM, ItemType.STRUCT, ItemType.DEF };
			foreach (ItemType type in complexTypes)
			{
				items = lib.List(type);
				foreach (Item item in items)
				{
					GenerateComplexItem(item);
				}
			}
			// Generate every function, all the global functions are added to the 'Main' class
			items = lib.List(ItemType.FUNCTION);
			// Get the parent namespace
			foreach (Function f in items)
			{
				string mainName;
				CodeTypeDeclaration main;
				CodeNamespace parent;

				Console.WriteLine("Generating function '" + f.Name + "'");
				parent = (CodeNamespace)GenerateParentObjects(f);
				if (parent == null)
				{
					Console.WriteLine("[ERR] Impossible to generate parent for '" + f.Name + "'");
				}
				else
				{
					mainName = parent.Name + ".Main";

					// Create our Main class
					if (processed.ContainsKey(mainName))
						main = (CodeTypeDeclaration)processed[mainName];
					else
					{
						main = new CodeTypeDeclaration("Main");
						processed[mainName] = main;
						parent.Types.Add(main);
					}
					main.Members.Add(GeneratePinvoke(f));
					CodeMemberMethod ret = GenerateFunction(f);
					main.Members.Add(ret);
				}
			}
			return cu;
		}

		private static System.Type GenerateType(string stype)
		{
			if (stype == "bool")
				return typeof(bool);
			else if (stype == "byte")
				return typeof(byte);
			else if (stype == "sbyte")
				return typeof(sbyte);
			else if (stype == "uint")
				return typeof(uint);
			else if (stype == "int")
				return typeof(int);
			else if (stype == "ulong")
				return typeof(ulong);
			else if (stype == "long")
				return typeof(long);
			else if (stype == "double")
				return typeof(double);
			else if (stype == "string")
				return typeof(string);
			else if (stype == "IntPtr")
				return typeof(System.IntPtr);
			else
				return null;
		}
	}
}
