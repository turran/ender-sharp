using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

/*
 * For callbacks, dont create pinvoke functions
 * For functions/methods that have a callback parameter, we need to:
 * 1. make the pinvoke be in the form static extern foo(int a, int b, FunctionCb);
 *    where FunctionCb must be the internal version of the callbackw with the C proto (done)
 * 2. create a delegate on the class with the C# form (done)
 * 3. On the body of foo, create an anonymous function that will translate the stuff
 *    from C# to C and viceversa
      Enesim.Renderer.DamageInternal cbinternal = (IntPtr ir, IntPtr iarea, bool ipast, IntPtr idata) => {
          return cb(new Enesim.Renderer(ir, false), new Eina.Rectangle(), ipast, idata);
      };
      bool ret = enesim_renderer_damages_get(raw, cbinternal, data);
 */

namespace Ender
{
	public class Generator
	{
		private CodeCompileUnit cu;
		private CodeNamespace root;
		private Lib lib;
		private Dictionary<string, CodeObject> processed;
		private System.Collections.Generic.List<string> skip;
		private CodeDomProvider provider;

		public Generator(Lib lib, CodeDomProvider provider, System.Collections.Generic.List<string> skip)
		{
			this.lib = lib;
			// Create our empty compilation unit
			cu = new CodeCompileUnit();
			// Our dictionary to keep processed items in sync
 			processed = new Dictionary<string, CodeObject>();
			// Our code provider to identify keywords
			this.provider = provider;
			// Our items to skip
			this.skip = skip;
		}

		public string ConvertFullName(string name)
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

		public string ConvertName(string id)
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
				case ItemType.STRUCT:
				case ItemType.OBJECT:
				case ItemType.DEF:
				case ItemType.BASIC:
				case ItemType.ENUM:
					return i.UnmanagedType(this, direction, transfer);
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					return "IntPtr";
				// TODO same as basic?
				case ItemType.CONSTANT:
					return "IntPtr";
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
				case ItemType.OBJECT:
				case ItemType.STRUCT:
				case ItemType.BASIC:
				case ItemType.ENUM:
				case ItemType.DEF:
					ret = i.UnmanagedType(this, direction, transfer);
					break;
				case ItemType.FUNCTION:
					ret = ConvertFullName(i.Name) + "Internal";
					break;
				// TODO same as basic?
				case ItemType.CONSTANT:
					ret = null;
					break;
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
			string pinvoke = null;

			// Handle the return value
			string retString = GenerateRetPinvoke(f.Ret);
			// Handle the args
			string argsString = GenerateArgsPinvoke(f);
			// For callbacks we need to generate a delegate using the C form (pinvoke)
			// We just change the name here
			if ((f.Flags & FunctionFlag.CALLBACK) != FunctionFlag.CALLBACK)
			{
				// Handle the function name
				string fName = GenerateNamePinvoke(f);
				pinvoke += string.Format("[DllImport(\"{0}.dll\", CallingConvention=CallingConvention.Cdecl)]", lib.Name);
				pinvoke += string.Format("\nprivate static extern {0} {1}({2});", retString, fName, argsString);
			}
			else
			{
				string fName = ConvertName(f.Identifier) + "Internal";
				pinvoke += string.Format("\ninternal delegate {0} {1}({2});", retString, fName, argsString);
			}
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
				case ItemType.DEF:
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
		private CodeStatementCollection GenerateArgPostStatementFull(Item i, string name, ArgDirection direction, ItemTransfer transfer)
		{
			switch (i.Type)
			{
				case ItemType.DEF:
				case ItemType.OBJECT:
					return i.ManagedPostStatements(this, name, direction, transfer);
				default:
					return null;
			}
		}

		// The statements needed to convert an arg from Pinvoke to C#
		private CodeStatementCollection GenerateArgPostStatement(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
				return null;
			return GenerateArgPostStatementFull(i, arg.Name, arg.Direction, arg.Transfer);
		}

		// bool ret;
		// Enesim.Renderer rSharp = new Enesim.Renderer(r, true);
          	// ret = cb(rSharp, data);
		// return ret;
		private CodeStatementCollection GenerateCallbackBody(Function f, string cbName)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			List args = f.Args;
			// Now the pre return statements
			if (args != null)
			{
				foreach (Arg a in args)
				{
					// Add any pre statement we might need
					CodeStatementCollection cs = GeneratePinvokeArgPreStatement(a);
					if (cs != null)
						csc.AddRange(cs);
				}
			}

			// Call the real callback
			// We for sure call the pinvoke function
			CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
			ci.Method = new CodeMethodReferenceExpression(null, cbName);

			// Now generate the cb args
			if (args != null)
			{
				foreach (Arg a in args)
				{
					// Add the expression to the invoke function
					CodeExpression ce = GeneratePinvokeArgExpression(a);
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
				Item argType = f.Ret.ArgType;
				string retType = argType.UnmanagedType(this, f.Ret.Direction, f.Ret.Transfer);
				cvs = new CodeVariableDeclarationStatement(retType, "retSharp", ci);
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
				foreach (Arg a in args)
				{
					// Add any pre statement we might need
					CodeStatementCollection cs = GenerateArgPostStatement(a);
					if (cs != null)
						csc.AddRange(cs);
				}
			}

			// Finally the return value
			if (ret != null)
			{
				CodeStatement cs = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retSharp"));
				csc.Add(cs);
			}

			return csc;

		}

		// Create an internal delegate
		private CodeStatementCollection GenerateArgPreStatementFunction(Function f, string argName)
		{
			// Generate the args of the function, this function differes from 
			string argsString = GenerateArgsPinvoke(f);
			StringWriter bodyWriter = new StringWriter();
			CodeStatementCollection csc = GenerateCallbackBody(f, argName);
			foreach (CodeStatement cs in csc) {
				provider.GenerateCodeFromStatement(cs, bodyWriter, new CodeGeneratorOptions());
			}
			string delegateString = string.Format("\n{0} {1} = ({2}) => {{\n{3}\n}};",
					ConvertFullName(f.Name) + "Internal", argName + "Internal", argsString,
					bodyWriter.ToString());

			csc = new CodeStatementCollection();
			csc.Add(new CodeSnippetStatement(delegateString));
			return csc;
		}

		// The statements needed to convert an arg from C# to Pinvoke
		private CodeStatementCollection GenerateArgPreStatementFull(Item i, string iName, string argName, ArgDirection direction, ItemTransfer transfer)
		{
			switch (i.Type)
			{
				case ItemType.DEF:
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					return i.ManagedPreStatements(this, argName, direction, transfer);
				// For function callbacks, we create a delegate
				case ItemType.FUNCTION:
					return GenerateArgPreStatementFunction((Function)i, argName);
				default:
					return null;
			}
		}

		// The statements needed to convert an arg from C# to Pinvoke
		private CodeStatementCollection GenerateArgPreStatement(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
				return null;
			return GenerateArgPreStatementFull(i, i.Name, arg.Name, arg.Direction, arg.Transfer);
		}

		private CodeExpression GeneratePinvokeArgExpression(Arg arg)
		{
			CodeExpression ret = null;
			Item i = arg.ArgType;

			if (i == null)
				return null;

			switch (i.Type)
			{
				case ItemType.OBJECT:
				ret = new CodeVariableReferenceExpression(arg.Name + "Sharp");
				break;

				case ItemType.BASIC:
				ret = new CodeVariableReferenceExpression(arg.Name);
				break;

				default:
				break;
			}
			return ret;
		}

		private CodeStatementCollection GeneratePinvokeArgPreStatement(Arg arg)
		{
			Item i = arg.ArgType;

			if (i == null)
				return null;

			CodeStatementCollection csc = new CodeStatementCollection();
			switch (i.Type)
			{
				case ItemType.OBJECT:
					// Call the constructor
					// identifier nameSharp;
					csc.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(ConvertName(i.Identifier)), arg.Name + "Sharp"));
					// nameSharp = new identifier(arg.Name, false/true);
					csc.Add(new CodeAssignStatement(new CodeVariableReferenceExpression(arg.Name + "Sharp"),
							new CodeObjectCreateExpression(new CodeTypeReference(ConvertName(i.Identifier)),
							new CodeVariableReferenceExpression(arg.Name),
							new CodePrimitiveExpression(true))));
					break;
				default:
					return null;
			}
			return csc;
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
					if (d.DefType.Type == ItemType.BASIC && direction == ArgDirection.OUT)
					{
						return GenerateArgExpressionFull(d.DefType, name + "Raw", direction, transfer);
					}
					else
					{
						return GenerateArgExpressionFull(d.DefType, name, direction, transfer);
					}
				case ItemType.FUNCTION:
					ret = new CodeVariableReferenceExpression(name + "Internal");
					break;
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
					if (direction == ArgDirection.OUT)
					{
						cvr = new CodeVariableReferenceExpression();
						cvr.VariableName = provider.CreateValidIdentifier(name);
						ret = new CodePropertyReferenceExpression(cvr, "Raw");
					}
					else
					{
						ret = new CodeVariableReferenceExpression(name + "Raw");
					}
					break;
				case ItemType.OBJECT:
					ret = new CodeVariableReferenceExpression(name + "Raw");
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

		private CodeParameterDeclarationExpression GenerateArg(Arg arg)
		{
			CodeParameterDeclarationExpression ret = null;
			Item i = arg.ArgType;

			if (i == null)
			{
				Console.WriteLine("[ERR] Arg '" + arg.Name + "' without a type?");
				ret = new CodeParameterDeclarationExpression();
				ret.Type = new CodeTypeReference("System.IntPtr");
				ret.Name = provider.CreateValidIdentifier(arg.Name);
				return ret;
			}

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
				case ItemType.FUNCTION:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(ConvertFullName(i.Name));
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
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(ConvertFullName(i.Name));
					break;
				default:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference("System.IntPtr");
					break;
			}

			if (ret == null)
				return ret;
			if (arg.Direction == ArgDirection.OUT)
				ret.Direction = FieldDirection.Out;
			ret.Name = provider.CreateValidIdentifier(arg.Name);
			return ret;
		}

		// TODO remove this to be an GenerateConstructor(generator, ... CodeExpression);
		// TODO and move the return to where it belongs actually
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
				case ItemType.DEF:
				case ItemType.ENUM:
				case ItemType.OBJECT:
					ret = new CodeMethodReturnStatement(i.Construct(this, "ret", arg.Direction, arg.Transfer));
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

			return new CodeTypeReference(i.ManagedType(this));
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
					CodeStatementCollection cs = GenerateArgPreStatement(a);
					if (cs != null)
						csc.AddRange(cs);
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
				Item argType = f.Ret.ArgType;
				string retType = argType.UnmanagedType(this, f.Ret.Direction, f.Ret.Transfer);
				cvs = new CodeVariableDeclarationStatement(retType, "ret", ci);
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
					CodeStatementCollection cs = GenerateArgPostStatement(a);
					if (cs != null)
						csc.AddRange(cs);
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
		private CodeTypeMember GenerateFunction(Function f)
		{
			CodeTypeMember cm = null;
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
			else if ((f.Flags & FunctionFlag.CALLBACK) == FunctionFlag.CALLBACK)
			{
				cm = new CodeTypeDelegate();
				cm.Name = ConvertName(f.Identifier);
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final;
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
						{
							if ((f.Flags & FunctionFlag.CALLBACK) == FunctionFlag.CALLBACK)
							{
								((CodeTypeDelegate)cm).Parameters.Add(cp);
							}
							else
							{
								((CodeMemberMethod)cm).Parameters.Add(cp);
							}
						}
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
						if ((f.Flags & FunctionFlag.CALLBACK) == FunctionFlag.CALLBACK)
						{
							((CodeTypeDelegate)cm).ReturnType = ret;
						}
						else
						{
							((CodeMemberMethod)cm).ReturnType = ret;
						}
					}
				}
			}

			if ((f.Flags & FunctionFlag.CALLBACK) != FunctionFlag.CALLBACK)
				((CodeMemberMethod)cm).Statements.AddRange(GenerateFunctionBody(f));

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

		private CodeStatementCollection GenerateFieldAssignment(Attr f, CodeExpression dst, CodeExpression src)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			Item i = f.AttrType;

			switch (i.Type)
			{
				// Impossible cases
				case ItemType.INVALID:
				case ItemType.ATTR:
				case ItemType.ARG:
				case ItemType.CONSTANT:
					break;
				// Basic case
				case ItemType.ENUM:
				case ItemType.BASIC:
				case ItemType.DEF:
					csc.Add(new CodeAssignStatement(dst, src));
					break;
				// TODO how to handle a function ptr?
				case ItemType.FUNCTION:
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					break;
				default:
					break;
			}
			return csc;
		}

		// TODO The struct should have every field on the inner
		// struct repeated. And a constructor based on a raw and a constructor
		// generic. In case of an out param being a struct, allocate the
		// unmanaged memory, call the function and then set the class members
		// Another option is at is right now, always have an unmanaged memory
		// and use that
		private CodeTypeDeclaration GenerateStruct(Struct s)
		{
			CodeMemberMethod cm;
			CodeMethodInvokeExpression cms;
			CodeMethodInvokeExpression cma;
			CodeParameterDeclarationExpression cmp;;

			Console.WriteLine("Generating struct " + s.Name);
			// Get the real item name
			CodeTypeDeclaration co = new CodeTypeDeclaration(ConvertName(s.Identifier));
			// Add the generated type into our hash
			processed[s.Name] = co;

			// Add a public class method to create the raw from its internal structure
			cm = new CodeMemberMethod();
			cm.Name = "CreateRaw";
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
			// IntPtr raw = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(p)))
			// Marshal.StructureToPtr(rawStruct, raw, false);
			// return raw
			cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.IntPtr"), "raw"));
			cms = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "SizeOf", new CodeTypeOfExpression(new CodeTypeReference(ConvertName(s.Identifier) + "Struct")));
			cma = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "AllocHGlobal", cms);
			cm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("raw"), cma));
			cms = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "StructureToPtr", new CodeExpression[] {
						new CodeVariableReferenceExpression("rawStruct"),
						new CodeVariableReferenceExpression("raw"),
						new CodePrimitiveExpression("false")
						});
			cm.Statements.Add(cms);
			cm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("raw")));
			cm.ReturnType = new CodeTypeReference("System.IntPtr");
			co.Members.Add(cm);

			// Add a public class method to destroy a raw
			cm = new CodeMemberMethod();
			cm.Name = "DestroyRaw";
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
			// rawStruct = Marshal.StructureToPtr(raw);
			cma = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "PtrToStructure", new CodeExpression[] {
						new CodeVariableReferenceExpression("rawStruct"),
						new CodeTypeOfExpression(new CodeTypeReference(ConvertName(s.Identifier) + "Struct"))
						});
			cm.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("rawStruct"), cma));
			// Marshal.FreeHGlobal(raw)
			cma = new CodeMethodInvokeExpression(
					new CodeTypeReferenceExpression("Marshal"), "FreeHGlobal", new CodeVariableReferenceExpression("raw"));
			cm.Statements.Add(new CodeExpressionStatement(cma));
			cmp = new CodeParameterDeclarationExpression("System.IntPtr", "raw");
			cm.Parameters.Add(cmp);
			co.Members.Add(cm);

			// Add the inner struct
			CodeTypeDeclaration cs = new CodeTypeDeclaration(ConvertName(s.Identifier) + "Struct");
			cs.Attributes = MemberAttributes.Private;
			cs.IsStruct = true;
			// Add the internal struct member rawStruct
			CodeMemberField rawStructField = new CodeMemberField(ConvertName(s.Identifier) + "Struct", "rawStruct");
			co.Members.Add(rawStructField);
			// Add the custom attributes [StructLayout(LayoutKind.Sequential)]
			cs.CustomAttributes.Add(new CodeAttributeDeclaration("StructLayout",
					new CodeAttributeArgument(new CodeFieldReferenceExpression(
					new CodeTypeReferenceExpression(typeof(LayoutKind)), "Sequential"))));
			List fields = s.Fields;
			if (fields != null)
			{
				foreach (Attr f in fields)
				{
					// Getter/Setter statements
					CodeStatementCollection csc;
					// Add the fields to the inner struct
					CodeMemberField mf = GenerateInnerField(f);
					if (mf != null)
						cs.Members.Add(mf);
					// Add the property to the outer class
					CodeMemberProperty fProp = new CodeMemberProperty();
					fProp.Attributes = MemberAttributes.Public | MemberAttributes.Final;
					fProp.Name = ConvertName(f.Name);
					fProp.Type = GenerateProp(f);
					fProp.HasGet = true;
					fProp.HasSet = true;
					Item fType = f.AttrType;

					// The getter
					// Enesim.Renderer ret
					string retType = fType.ManagedType(this);
					string retName = "ret";
					fProp.GetStatements.Add(new CodeVariableDeclarationStatement(retType, "ret"));
					csc = fType.ManagedPreStatements(this, "ret", ArgDirection.OUT, ItemTransfer.NONE);
					if (csc != null)
					{
						fProp.GetStatements.AddRange(csc);
						retName = "retRaw";
					}
					csc = GenerateFieldAssignment(f, new CodeVariableReferenceExpression(retName),
							new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(), "rawStruct"), f.Name));
					if (csc != null)
					{
						fProp.GetStatements.AddRange(csc);
					}
					fProp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("ret")));

					csc = fType.ManagedPreStatements(this, "value", ArgDirection.IN, ItemTransfer.NONE);
					if (csc != null)
					{
						fProp.SetStatements.AddRange(csc);
					}
					csc = GenerateFieldAssignment(f, new CodeFieldReferenceExpression(new CodeFieldReferenceExpression(
							new CodeThisReferenceExpression(), "rawStruct"), f.Name),
							new CodePropertySetValueReferenceExpression());
					if (csc != null)
					{
						fProp.SetStatements.AddRange(csc);
					}
					co.Members.Add(fProp);
				}
			}
			co.Members.Add(cs);
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

		// For basic types, something like this:
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
		private CodeTypeDeclaration GenerateBasicDef(Def d, Basic b)
		{
			CodeTypeDeclaration co = new CodeTypeDeclaration(ConvertName(d.Identifier));
			CodeTypeReference ct = GenerateBasic(b);
			// Add the basic type as a Value member
			CodeMemberField valueField = new CodeMemberField(ct, "value");
			valueField.Attributes = MemberAttributes.Family;
			co.Members.Add(valueField);
			// Add the getter
			CodeMemberProperty valueProp = new CodeMemberProperty();
			valueProp.Name = "Value";
			valueProp.Type = GenerateBasic(b);
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
			cmm.Name = "implicit operator " + b.UnmanagedType(this, ArgDirection.IN, ItemTransfer.FULL);
			cmm.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			cmm.ReturnType = new CodeTypeReference(" ");
			cmm.Parameters.Add(new CodeParameterDeclarationExpression(ConvertName(d.Identifier), "v"));
			cmm.Statements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeVariableReferenceExpression("v"), "value")));
			co.Members.Add(cmm);

			return co;
		}

		private CodeTypeDeclaration GenerateDef(Def d)
		{
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
					co = GenerateBasicDef(d, (Basic)i);
 					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
				case ItemType.ENUM:
				case ItemType.DEF:
					// For complex types, we might inherit directly
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
			if (co == null)
				return null;

			// Add the generated type into our hash
			processed[d.Name] = co;

			// Create every pinvoke for functions
			List funcs = d.Functions;
			if (funcs != null)
			{
				foreach (Function f in funcs)
				{
					Console.WriteLine("Processing PInvoke function " + f.Name);
					co.Members.Add(GeneratePinvoke(f));
				}
			}

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
					CodeTypeMember cm = GenerateFunction(f);
					if (cm != null)
						co.Members.Add(cm);
				}
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
			bool isPartial = false;

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
						if (skip.Contains(f.FullName))
						{
							Console.WriteLine("Skipping function '" + f.FullName + "'");
							continue;
						}

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
					if (skip.Contains(f.FullName))
					{
						Console.WriteLine("Skipping function '" + f.FullName + "'");
						isPartial = true;
						continue;
					}
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
			if (isPartial)
				co.IsPartial = true;


			// Generate every function
			if (funcs != null)
			{
				foreach (Function f in funcs)
				{
					if (skip.Contains(f.FullName))
					{
						Console.WriteLine("Skipping function '" + f.FullName + "'");
						continue;
					}
					// Skip the ref/unref
					if ((f.Flags & FunctionFlag.REF) == FunctionFlag.REF)
						continue;
					if ((f.Flags & FunctionFlag.UNREF) == FunctionFlag.UNREF)
						continue;

					Console.WriteLine("Generating function " + f.Name);
					CodeTypeMember cm = GenerateFunction(f);
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

			// It might be possible that generating parent objects creates ourselves
			if (processed.ContainsKey(item.Name))
				return processed[item.Name];

			// Check if we need to skip this item
			if (skip.Contains(item.Name))
			{
				Console.WriteLine("Skipping item '" + item.Name + "'");
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
			// Generate every complex type (object, structs, enum and defs)
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
			// Get the parent namespace or object for functions and callbacks
			foreach (Function f in items)
			{
				string mainName;
				CodeTypeDeclaration main;
				CodeObject parent;

				if (skip.Contains(f.Name))
				{
					Console.WriteLine("Skipping function '" + f.Name + "'");
					continue;
				}

				Console.WriteLine("Generating function '" + f.Name + "'");
				parent = GenerateParentObjects(f);
				if (parent == null)
				{
					Console.WriteLine("[ERR] Impossible to generate parent for '" + f.Name + "'");
				}
				else
				{
					CodeSnippetTypeMember pinvoke = null;

					pinvoke = GeneratePinvoke(f);

					// For functions on namespace add the Main
					if (parent.GetType() == typeof(CodeNamespace))
					{
						CodeNamespace ns = (CodeNamespace)parent;

						mainName = ns.Name + ".Main";

						// Create our Main class
						if (processed.ContainsKey(mainName))
							main = (CodeTypeDeclaration)processed[mainName];
						else
						{
							main = new CodeTypeDeclaration("Main");
							processed[mainName] = main;
							ns.Types.Add(main);
						}

						if (pinvoke != null)
							main.Members.Add(pinvoke);

						CodeTypeMember ret = GenerateFunction(f);
						main.Members.Add(ret);
					}
					// For functions on an object just add it
					else
					{
						CodeTypeDeclaration ty = (CodeTypeDeclaration)parent;
						if (pinvoke != null)
							ty.Members.Add(pinvoke);
						CodeTypeMember ret = GenerateFunction(f);
						ty.Members.Add(ret);
					}
				}
			}
			return cu;
		}
	}
}
