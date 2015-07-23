using System;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;
using System.CodeDom;
using System.CodeDom.Compiler;

namespace Ender
{
	[Flags]
	public enum FunctionFlag
	{
		IS_METHOD = (1 << 0),
		THROWS    = (1 << 1),
		CTOR      = (1 << 2),
		REF       = (1 << 3),
		UNREF     = (1 << 4),
		CALLBACK  = (1 << 5),
		VALUE_OF  = (1 << 6),
	}

	public class Function : Item
	{
		/* ender_item_function.h */
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_args_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern IntPtr ender_item_function_ret_get(IntPtr i);
		[DllImport("libender.dll")]
		static extern FunctionFlag ender_item_function_flags_get(IntPtr i);
		/*
		EAPI Ender_Item * ender_item_function_args_at(Ender_Item *i, int idx);
		EAPI int ender_item_function_args_count(Ender_Item *i);
		Eina_Bool ender_item_function_call(Ender_Item *i,
				Ender_Value *args, Ender_Value *retval);
		EAPI int ender_item_function_throw_position_get(Ender_Item *i);
		*/
		internal Function()
		{
		}

		internal Function(IntPtr p) : this(p, true)
		{
		}

		public Function(IntPtr p, bool owned) : base(p, owned)
		{
		}

		public List Args
		{
			get {
				IntPtr l = ender_item_function_args_get(raw);
				if (l == IntPtr.Zero)
					return null;

				List list = new List(l, typeof(Arg), true, true);
				return list;
			}
		}

		public Arg Ret
		{
			get {
				IntPtr i = ender_item_function_ret_get(raw);
				if (i == IntPtr.Zero)
					return null;

				return new Arg(i, true);
			}
		}

		public FunctionFlag Flags
		{
			get {
				return ender_item_function_flags_get(raw);
			}
		}

		// TODO make this private
		public string GeneratePinvokeArgs(Generator generator)
		{
			string ret;
			List args = Args;

			if (args == null)
				return null;

			string[] argsString = new string[args.Count];
			// Generate each arg string
			for (uint i = 0; i < args.Count; i++)
			{
				Arg a = (Arg)args.Nth(i);
				argsString[i] = a.GeneratePinvoke(generator);
			}
			ret = String.Join(", ", argsString);
			return ret;
		}

		// Generate the name of the C function, like:
		// enesim_renderer_name_get
		public string GeneratePinvokeName(Generator generator)
		{
			Item parent = Parent;
			if (parent == null)
			{
				// TODO use the correct replacement to support case/notation
				return Name.Replace(".", "_");
			}
			else
			{
				string fName;
				// TODO use the correct replacement to support case/notation
				// in case the parent is an attribute, we will have another parent
				if (parent.Type == ItemType.ATTR)
				{
					fName = parent.Parent.Name.Replace(".", "_") + "_" + parent.Name + "_" + Identifier;
				}
				else
				{
					fName = parent.Namespace.Replace(".", "_") + "_" + parent.Identifier + "_" + Identifier;
				}
				return fName;
			}
		}

		// Generate the declaration of the C function, like:
		// [DllImport("enesim.dll", CallingConvention=CallingConvention.Cdecl)]
		// private static extern System.IntPtr enesim_renderer_name_get(System.IntPtr selfRaw);
		public CodeSnippetTypeMember GeneratePinvoke(Generator generator)
		{
			string pinvoke = null;

			// Handle the return value
			string retString = (Ret != null) ? Ret.GenerateRetPinvoke(generator) : "void";
			// Handle the args
			string argsString = GeneratePinvokeArgs(generator);
			// For callbacks we need to generate a delegate using the C form (pinvoke)
			// We just change the name here
			if ((Flags & FunctionFlag.CALLBACK) != FunctionFlag.CALLBACK)
			{
				// Handle the function name
				string fName = GeneratePinvokeName(generator);
				pinvoke += string.Format("[DllImport(\"{0}.dll\", CallingConvention=CallingConvention.Cdecl)]", generator.Lib.Name);
				pinvoke += string.Format("\nprivate static extern {0} {1}({2});", retString, fName, argsString);
			}
			else
			{
				string fName = generator.ConvertName(Identifier) + "Internal";
				pinvoke += string.Format("\ninternal delegate {0} {1}({2});", retString, fName, argsString);
			}
			CodeSnippetTypeMember ext = new CodeSnippetTypeMember(pinvoke);
			return ext;
		}


		private CodeStatementCollection GenerateCallbackArgsPreStatements(Generator generator)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			List args = Args;

			if (args == null)
				return csc;

			foreach (Arg a in args)
			{
				Item i = a.ArgType;
				if (i == null)
					continue;

				CodeStatementCollection cs = i.UnmanagedPreStatements(generator, a.Name,
						a.Direction, a.Transfer); 
				if (cs != null)
					csc.AddRange(cs);
			}
			return csc;
		}

		// Generate the body of a callback
		private CodeStatementCollection GenerateCallbackBody(Generator generator, string cbName)
		{
			CodeStatementCollection csc = new CodeStatementCollection();
			List args = Args;

			// Now the pre return statements
			csc.AddRange(GenerateCallbackArgsPreStatements(generator));

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
					CodeExpression ce = new CodeVariableReferenceExpression(a.Name);
					if (ce != null)
						ci.Parameters.Add(ce);
				}
			}

			// Now the return value prototype
			CodeTypeReference ret = GenerateRet(generator);
			if (ret != null)
			{
				CodeVariableDeclarationStatement cvs;
				// Add the return value
				Arg argRet = Ret;
				Item argType = argRet.ArgType;
				string retType = argType.UnmanagedType(generator, argRet.Direction, argRet.Transfer);
				cvs = new CodeVariableDeclarationStatement(retType, "retInternal", ci);
				csc.Add(cvs);
			}
			else
			{
				// Just call the method
				CodeStatement cs = new CodeExpressionStatement(ci);
				csc.Add(cs);
			}
			// Finally the return value
			if (ret != null)
			{
				CodeStatement cs = new CodeMethodReturnStatement(new CodeVariableReferenceExpression("retInternal"));
				csc.Add(cs);
			}
			return csc;
		}

		public CodeTypeReference GenerateRet(Generator generator)
		{
			Arg arg = Ret;
			if (arg == null)
				return null;
			return arg.GenerateRet(generator);
		}

		#region Item interface
		public override string ManagedType(Generator generator)
		{
			return FullQualifiedName;
		}

		public override string UnmanagedType(Generator generator,
				ArgDirection direction, ItemTransfer transfer)
		{
			return FullQualifiedName + "Internal";
		}

		public override string UnmanagedName(string name,
				ArgDirection direction, ItemTransfer transfer)
		{
			return name + "Raw";
		}

		// Create an internal delegate in the form
		// Enesim.Image.CallbackInternal varName = (System.IntPtr rRaw,System.Boolean success) => {
		// }
		public override CodeStatementCollection ManagedPreStatements(
				Generator generator, string varName,
				ArgDirection direction, ItemTransfer transfer)
		{
			// Generate the args of the function
			string argsString = GeneratePinvokeArgs(generator);
			StringWriter bodyWriter = new StringWriter();
			CodeStatementCollection csc = GenerateCallbackBody(generator, varName);
			foreach (CodeStatement cs in csc) {
				generator.Provider.GenerateCodeFromStatement(cs, bodyWriter, new CodeGeneratorOptions());
			}
			string unmanagedType = UnmanagedType(generator, ArgDirection.IN, ItemTransfer.NONE);
			string unmanagedName = UnmanagedName(varName, ArgDirection.IN, ItemTransfer.NONE);
			string delegateString = string.Format("\n{0} {1} = ({2}) => {{\n{3}\n}};",
					unmanagedType, unmanagedName, argsString, bodyWriter.ToString());

			csc = new CodeStatementCollection();
			csc.Add(new CodeSnippetStatement(delegateString));
			return csc;
		}
	
		public override void QualifiedName(out string className, out string nsName)
		{
			Item parent = Lib.FindItem(Namespace);

			if (parent == null)
			{
				base.QualifiedName(out className, out nsName);
				nsName = nsName + ".Main";
			}
			else
			{
				parent.QualifiedName(out className, out nsName);
				nsName = nsName + "." + className;
				className = Utils.Convert(Identifier, Utils.Case.UNDERSCORE, Lib.Notation,
					Utils.Case.PASCAL, Utils.Notation.ENGLISH);
			}
		}
		#endregion
	}
}


