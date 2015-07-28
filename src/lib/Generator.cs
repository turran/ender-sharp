using System;
using System.IO;
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

		public CodeDomProvider Provider {
			get {
				return provider;
			}
		}

		public Lib Lib {
			get {
				return lib;
			}
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
							new CodeExpressionStatement(new CodeMethodInvokeExpression(null, unrefFunc.GeneratePinvokeName(this), new CodeVariableReferenceExpression("raw"))),
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
				CodeSnippetTypeMember ext = new CodeSnippetTypeMember("~" + co.Name + "() { " + unrefFunc.GeneratePinvokeName(this) + "(); }");
				co.Members.Add(ext);
			}
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
					ret.Type = new CodeTypeReference(i.ManagedType(this));
					break;
				case ItemType.FUNCTION:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(i.ManagedType(this));
					break;
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeParameterDeclarationExpression();
					ret.Type = new CodeTypeReference(i.ManagedType(this));
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
						ret.Type = new CodeTypeReference(i.ManagedType(this));
					}
					else
					{
						ret = new CodeParameterDeclarationExpression();
						ret.Type = new CodeTypeReference(i.ManagedType(this));
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
					CodeStatementCollection cs = a.ManagedPreStatements(this);
					if (cs != null)
						csc.AddRange(cs);
				}
			}

			// We for sure call the pinvoke function
			CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
			ci.Method = new CodeMethodReferenceExpression(null, f.GeneratePinvokeName(this));

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
					CodeExpression ce = a.GenerateExpression(this);
					if (ce != null)
						ci.Parameters.Add(ce);
				}
			}

			// Now the return value prototype
			CodeTypeReference ret = f.GenerateRet(this);
			if (ret != null)
			{
				CodeVariableDeclarationStatement cvs;
				string retType = "IntPtr";

				Item argType = f.Ret.ArgType;
				if (argType != null)
					retType = argType.UnmanagedType(this, f.Ret.Direction, f.Ret.Transfer);
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
					CodeStatementCollection cs = a.ManagedPostStatements(this);
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
			CodeTypeReference ret = f.GenerateRet(this);
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
			string className;
			string nsName;
			e.QualifiedName(out className, out nsName);
			CodeTypeDeclaration co = new CodeTypeDeclaration(className);

			CodeTypeDeclaration coEnum;
			if (e.Functions != null)
			{
				coEnum = new CodeTypeDeclaration("Enum");
				co.Members.Add(coEnum);
			}
			else
			{
				coEnum = co;
			}
			// Get the values
			List values = e.Values;
			if (values != null)
			{
				foreach (Constant c in values)
				{
					CodeMemberField f = new CodeMemberField ();
					f.Name = ConvertName(c.Identifier);

					// Add the value
					Value v = c.Value;
					f.InitExpression = new CodePrimitiveExpression(v.I32);
         				coEnum.Members.Add(f);
				}
			}
			// Add the generated type into our hash
			processed[e.Name] = co;
			coEnum.IsEnum = true;
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
				case ItemType.FUNCTION:
					ret = new CodeMemberField();
					ret.Type = new CodeTypeReference(i.UnmanagedType(this, ArgDirection.IN, ItemTransfer.NONE));
					ret.Name = i.UnmanagedName(name, ArgDirection.IN, ItemTransfer.NONE);
					break;
				case ItemType.BASIC:
				case ItemType.ENUM:
				case ItemType.STRUCT:
				case ItemType.OBJECT:
					ret = new CodeMemberField();
					ret.Type = new CodeTypeReference(i.ManagedType(this));
					ret.Name = name;
					ret.Attributes = MemberAttributes.Public;
					break;
				// TODO same as basic?
				case ItemType.CONSTANT:
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
			CodeParameterDeclarationExpression cmp;

			Console.WriteLine("Generating struct " + s.Name);
			// Get the real item name
			string className;
			string nsName;
			s.QualifiedName(out className, out nsName);
			CodeTypeDeclaration co = new CodeTypeDeclaration(className);
			// Add the generated type into our hash
			processed[s.Name] = co;

			// Add a generic constructor
			CodeConstructor cc = new CodeConstructor();
			cc.Attributes = MemberAttributes.Public;
			co.Members.Add(cc);

			// Add a constructor to pass directly the raw
			cc = new CodeConstructor();
			cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "i"));
			cc.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
			// Marshal the raw
			// TODO In case is owned, do a copy
			cc.Statements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("rawStruct"),
					new CodeCastExpression(ConvertName(s.Identifier) + "Struct",
						new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Marshal"), "PtrToStructure", new CodeExpression[] {
							new CodeVariableReferenceExpression("i"),
							new CodeTypeOfExpression(new CodeTypeReference(ConvertName(s.Identifier) + "Struct"))
						})
					)));
			cc.Attributes = MemberAttributes.Public;
			co.Members.Add(cc);

			// Add a property to get/set Raw
			CodeMemberProperty cmprop = new CodeMemberProperty();
			cmprop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			cmprop.Name = ConvertName("Raw");
			cmprop.Type = new CodeTypeReference("IntPtr");
			cmprop.HasGet = true;
			cmprop.HasSet = true;
			// The getter
			// IntPtr raw = Matrix.CreateRaw()
			// Marshal.StructureToPtr(rawStruct, raw, false);
			// return raw;
			cmprop.GetStatements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.IntPtr"), "raw"));
			cmprop.GetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("raw"), new CodeMethodInvokeExpression(null, "CreateRaw")));
			cmprop.GetStatements.Add(new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Marshal"), "StructureToPtr", new CodeExpression[] {
						new CodeVariableReferenceExpression("rawStruct"),
						new CodeVariableReferenceExpression("raw"),
						new CodePrimitiveExpression(false)
						}));
			cmprop.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("raw")));
			// The setter
			// rawStruct = Marshal.StructureToPtr(raw);
			// Matrix.DestroyRaw()
			cmprop.SetStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("rawStruct"),
					new CodeCastExpression(ConvertName(s.Identifier) + "Struct", new CodeMethodInvokeExpression(new CodeTypeReferenceExpression("Marshal"), "PtrToStructure", new CodeExpression[] {
						new CodePropertySetValueReferenceExpression(),
						new CodeTypeOfExpression(new CodeTypeReference(ConvertName(s.Identifier) + "Struct"))
						}))));
			cmprop.SetStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(null, "DestroyRaw", new CodeExpression[] {
						new CodePropertySetValueReferenceExpression()
						})));
			co.Members.Add(cmprop);

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
			cm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("raw")));
			cm.ReturnType = new CodeTypeReference("System.IntPtr");
			co.Members.Add(cm);

			// Add a public class method to destroy a raw
			cm = new CodeMemberMethod();
			cm.Name = "DestroyRaw";
			cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
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
					fProp.Type = new CodeTypeReference(f.ManagedType(this));
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
					fProp.GetStatements.Add(new CodeMethodReturnStatement(fType.Construct(this, fType.UnmanagedName("ret", ArgDirection.IN, ItemTransfer.FULL), ArgDirection.IN, ItemTransfer.FULL)));

					// The setter
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
				ci.Method = new CodeMethodReferenceExpression(null, refFunc.GeneratePinvokeName(this));
				ci.Parameters.Add(new CodeVariableReferenceExpression("i"));
				CodeConditionStatement cs = new CodeConditionStatement(new CodeVariableReferenceExpression("owned"),
						new CodeExpressionStatement(ci));
				cm.Statements.Add(cs);
				co.Members.Add(cm);
			}
			else
			{
				// add the inheritance on the type
				co.BaseTypes.Add(inherit.FullQualifiedName);
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
				cc.Attributes = MemberAttributes.Public;
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
					cmp.Type = new CodeTypeReference(a.ManagedType(this));
					// For getters/setters we just generate the function body but we need
					// to declare the input params of the "function" to make it work
					f = a.Getter;
					if (f != null)
					{
						// Different possibilities:
						// type foo_get(o)
						// void foo_get(o, type *)
						// bool foo_get(o, type *, err)
						List args = f.Args;
						if (args.Count > 1)
						{
							Arg arg1 = (Arg)args.Nth(1);
							cmp.GetStatements.Add(new CodeVariableDeclarationStatement(cmp.Type, arg1.Name));
						}
						cmp.GetStatements.AddRange(GenerateFunctionBody(f));
						Item ret = f.Ret;
						if (args.Count > 1)
						{
							Arg arg1 = (Arg)args.Nth(1);
							cmp.GetStatements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression(arg1.Name)));
						}
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
			CodeTypeReference ct = new CodeTypeReference(b.ManagedType(this));
			// Add the basic type as a Value member
			CodeMemberField valueField = new CodeMemberField(ct, "value");
			valueField.Attributes = MemberAttributes.Family;
			co.Members.Add(valueField);
			// Add the getter
			CodeMemberProperty valueProp = new CodeMemberProperty();
			valueProp.Name = "Value";
			valueProp.Type = new CodeTypeReference(b.ManagedType(this));
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
					co.Members.Add(f.GeneratePinvoke(this));
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
			bool hasDowncast = false;
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
			string className;
			string nsName;
			o.QualifiedName(out className, out nsName);
			CodeTypeDeclaration co = new CodeTypeDeclaration(className);
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
					// Skip the downcast function, we'll call ender directly 
					if ((f.Flags & FunctionFlag.DOWNCAST) == FunctionFlag.DOWNCAST)
						continue;

					Console.WriteLine("Processing PInvoke function " + f.Name);
					co.Members.Add(f.GeneratePinvoke(this));
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
						co.Members.Add(f.GeneratePinvoke(this));
					}

					f = a.Setter;
					if (f != null)
					{
						Console.WriteLine("Processing Setter PInvoke function " + f.Name);
						co.Members.Add(f.GeneratePinvoke(this));
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
					if ((f.Flags & FunctionFlag.DOWNCAST) == FunctionFlag.DOWNCAST)
					{
						hasDowncast = true;
						continue;
					}

					Console.WriteLine("Generating function " + f.Name);
					CodeTypeMember cm = GenerateFunction(f);
					if (cm != null)
						co.Members.Add(cm);
				}
			}

			// Geneate the downcast function if needed
			if (hasDowncast)
			{
				// public static Enesim.Renderer Downcast(IntPtr raw, bool owned)
				CodeMemberMethod cm = new CodeMemberMethod();
				cm.Name = ConvertName("Downcast");
				cm.Attributes = MemberAttributes.Public | MemberAttributes.Final | MemberAttributes.Static;
				cm.ReturnType = new CodeTypeReference(o.ManagedType(this));
				cm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IntPtr), "raw"));
				cm.Parameters.Add(new CodeParameterDeclarationExpression(typeof(bool), "owned"));
				// Ender.Lib  lib = Ender.Lib.Find("enesim");
				CodeMethodInvokeExpression ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("Ender.Lib"), "Find");
				ci.Parameters.Add(new CodePrimitiveExpression(lib.Name));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Ender.Lib"), "lib", ci));
				// Object o = (Object)lib.FindItem(name);
				ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("lib"), "FindItem");
				ci.Parameters.Add(new CodePrimitiveExpression(o.Name));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Ender.Object"), "o",
						new CodeCastExpression("Ender.Object", ci)));
				// Item downcastedItem = o.Downcast(raw);
				ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("o"), "Downcast");
				ci.Parameters.Add(new CodeVariableReferenceExpression("raw"));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("Ender.Item"), "downO", ci));
				// Type downType = Type.GetType(downO.QualifiedName);
				ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression("System.Type"), "GetType");
				ci.Parameters.Add(new CodePropertyReferenceExpression(new CodeVariableReferenceExpression("downO"), "FullQualifiedName"));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Type"), "downType", ci));
				// Type[] types = new Type[2];
				CodeArrayCreateExpression types = new CodeArrayCreateExpression("System.Type", 2);
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Type[]"), "types", types));
				// Type[0] = typeof(IntPtr);
				cm.Statements.Add(new CodeAssignStatement(
						new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("types"), new CodePrimitiveExpression(0)),
						new CodeTypeOfExpression(new CodeTypeReference("IntPtr"))));
				// Type[1] = typeof(bool);
				cm.Statements.Add(new CodeAssignStatement(
						new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("types"), new CodePrimitiveExpression(1)),
						new CodeTypeOfExpression(new CodeTypeReference(typeof(bool)))));
				// ConstructorInfo ctorInfo = downcastedClass.GetConstructor(IntPtr, bool);
				ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("downType"), "GetConstructor");
				ci.Parameters.Add(new CodeVariableReferenceExpression("types"));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("ConstructorInfo"), "ctorInfo", ci));
				// Object[] objects = new Objects[2];
				CodeArrayCreateExpression objects = new CodeArrayCreateExpression("System.Object", 2);
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference("System.Object[]"), "objects", objects));
				// objects[0] = raw;
				cm.Statements.Add(new CodeAssignStatement(
						new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("objects"), new CodePrimitiveExpression(0)),
						new CodeVariableReferenceExpression("raw")));
				// objects[1] = owned;
				cm.Statements.Add(new CodeAssignStatement(
						new CodeArrayIndexerExpression(new CodeVariableReferenceExpression("objects"), new CodePrimitiveExpression(1)),
						new CodeVariableReferenceExpression("owned")));
				// Enesim.Renderer ret = ctorInfo.Invoke(objects);
				ci = new CodeMethodInvokeExpression();
				ci.Method = new CodeMethodReferenceExpression(new CodeVariableReferenceExpression("ctorInfo"), "Invoke");
				ci.Parameters.Add(new CodeVariableReferenceExpression("objects"));
				cm.Statements.Add(new CodeVariableDeclarationStatement(new CodeTypeReference(o.ManagedType(this)), "ret",
						new CodeCastExpression(new CodeTypeReference(o.ManagedType(this)), ci)));
				// return ret;
				cm.Statements.Add(new CodeMethodReturnStatement(new CodeVariableReferenceExpression("ret")));
				co.Members.Add(cm);
			}

			return co;
		}

		private void GenerateLib()
		{
			CodeNamespace root = new CodeNamespace();
			cu.Namespaces.Add(root);
			// Our default imports
			root.Imports.Add(new CodeNamespaceImport("Ender"));
			root.Imports.Add(new CodeNamespaceImport("System"));
			root.Imports.Add(new CodeNamespaceImport("System.Reflection"));
			root.Imports.Add(new CodeNamespaceImport("System.Runtime.InteropServices"));
			List deps = lib.Dependencies;
			if (deps != null)
			{
				foreach (Lib l in deps)
				{
					string name = l.Name.Replace("-", ".");
					root.Imports.Add(new CodeNamespaceImport(ConvertName(name)));
				}
			}
		}

		private CodeNamespace GenerateNamespace(Item item)
		{
			// generate the namespace the item belongs to
			string nsName;
			string className;

			item.QualifiedName(out className, out nsName);
			CodeNamespace cns;
			if (processed.ContainsKey(nsName))
			{
				cns = (CodeNamespace)processed[nsName];
			}
			else
			{
 				cns = new CodeNamespace(nsName);
				cu.Namespaces.Add(cns);
				processed[nsName] = cns;
			}
			return cns;
		}

		private CodeObject GenerateComplexItem(Item item)
		{
			CodeObject ret = null;

			// check if the item has been already processed
			if (processed.ContainsKey(item.Name))
				return processed[item.Name];

			CodeNamespace cns = GenerateNamespace(item);
			if (cns == null)
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
			cns.Types.Add((CodeTypeDeclaration)ret);
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
				if (skip.Contains(f.Name))
				{
					Console.WriteLine("Skipping function '" + f.Name + "'");
					continue;
				}
				Console.WriteLine("Generating function '" + f.Name + "'");

				CodeSnippetTypeMember pinvoke = f.GeneratePinvoke(this);
				if (pinvoke == null)
				{
					Console.WriteLine("[ERR] Impossible to generate the pinvoke for '" + f.Name + "'");
				}

				Item parent = Lib.FindItem(f.Namespace);
				if (parent == null)
				{
					CodeNamespace cns = GenerateNamespace(f);
					if (cns == null)
					{
						Console.WriteLine("[ERR] Impossible to generate namespace for '" + f.Name + "'");
					}
					else
					{
						CodeTypeDeclaration main;
						string mainName = cns.Name + ".Main";

						// Create our Main class
						if (processed.ContainsKey(mainName))
							main = (CodeTypeDeclaration)processed[mainName];
						else
						{
							main = new CodeTypeDeclaration("Main");
							processed[mainName] = main;
							cns.Types.Add(main);
						}
						CodeTypeMember ret = GenerateFunction(f);
						main.Members.Add(pinvoke);
						main.Members.Add(ret);
					}
				}
				else
				{
					CodeTypeDeclaration ty = (CodeTypeDeclaration)GenerateComplexItem(parent);
					if (ty == null)
					{
						Console.WriteLine("[ERR] Impossible to generate parent for '" + f.Name + "'");
					}
					else
					{
						CodeTypeMember ret = GenerateFunction(f);
						ty.Members.Add(pinvoke);
						ty.Members.Add(ret);
					}
				}
			}
			return cu;
		}
	}
}
