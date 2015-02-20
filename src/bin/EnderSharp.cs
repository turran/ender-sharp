using Ender;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

public class EnderSharp
{
	private static void Help()
	{
		// TODO we should add also a list of types that we want to skip
		// of generating
	}

	public static string generateCSharpCode(CodeCompileUnit cu, string outfile)
	{
		// Generate the code with the C# code provider.
		CSharpCodeProvider provider = new CSharpCodeProvider();

		// Build the output file name. 
		string sourceFile;
		if (provider.FileExtension[0] == '.')
		{
			sourceFile = outfile + provider.FileExtension;
		}
		else
		{
			sourceFile = outfile + "." + provider.FileExtension;
		}

		StreamWriter sw = new StreamWriter(sourceFile, false);
		IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
		provider.GenerateCodeFromCompileUnit(cu, tw,
				new CodeGeneratorOptions());
		tw.Close();

		return sourceFile;
	}


	public static void Main(string[] args)
	{
		// TODO do the args processing
		// initialize ender before we do anything
		Ender.Main.Init();
		Generator eg = new Generator(args[0]);
		CodeCompileUnit cu = eg.Generate();
		generateCSharpCode(cu, args[0]);
		Ender.Main.Shutdown();
	}
}
