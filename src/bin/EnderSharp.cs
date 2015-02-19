using Ender;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

public class EnderSharp
{
	private static void help()
	{
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
		/* TODO do the args processing */
		Generator eg = new Generator();
		CodeCompileUnit cu = eg.generate(args[0]);
		generateCSharpCode(cu, args[0]);
	}
}
