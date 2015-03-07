using Ender;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using Microsoft.CSharp;

/**
 * TODO:
 * - Add parameters to:
 *   define the lib to parse
 *   define any object/method/whatever that needs to be skipped
 *   for example enesim.renderer.shape.foo_get which is a method, will be
 *   searched as enesim.renderer.shape.foo_get, then enesim.renderer.shape + foo_get
 *   then and if found, pass the blacklisted items to the Generator. Once an item
 *   needs to be generated skip it
 *   Pass the package version name that will be written on the assemblyinfo.cs and the pkg-config file
 * - Generate the .pc file, which will generate the pkg-config file of the generated lib
 * - Generate the .dll.config to make it independent of the OS
 */

public class EnderSharp
{
	private static void Help()
	{
		// TODO we should add also a list of types that we want to skip
		// of generating
	}

	public static string GenerateCode(CodeDomProvider provider, CodeCompileUnit cu, string outfile)
	{
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
		// Generate the code with the C# code provider.
		CSharpCodeProvider provider = new CSharpCodeProvider();
		Generator eg = new Generator(args[0], provider);
		CodeCompileUnit cu = eg.Generate();
		GenerateCode(provider, cu, args[0]);
		Ender.Main.Shutdown();
	}
}
