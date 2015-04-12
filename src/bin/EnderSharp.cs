using Ender;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Microsoft.CSharp;
using Mono.Options;

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
	// The idea is to generate the .pc files
	public static string GeneratePcFile(Lib lib, string outfile)
	{
		string toReplace = @"prefix=${pcfiledir}/../..
		exec_prefix=${prefix}
		libdir=${exec_prefix}/lib

		Name: {LIB}-sharp
		Description: {LIB} .NET Binding
		Version: {VERSION}
		Libs: -r:${libdir}/cli/{LIB}.dll
		Requires: {DEPS}";

		string pcFile = toReplace.Replace("\t", "");
		pcFile = pcFile.Replace("{LIB}", lib.Name);
		pcFile = pcFile.Replace("{VERSION}", lib.Version.ToString());
		List deps = lib.Dependencies;
		if (deps != null)
		{
			StringBuilder builder = new StringBuilder();
			foreach (Lib l in deps)
			{
				builder.Append(l.Name + "-sharp").Append(",");
			}
			pcFile = pcFile.Replace("{DEPS}", builder.ToString().TrimEnd(','));
		}
		else
		{
			pcFile = pcFile.Replace("{DEPS}", "");
		}
		File.WriteAllText(outfile, pcFile);
		return pcFile;
	}

	public static void GenerateCode(CodeDomProvider provider, CodeCompileUnit cu, string sourceFile)
	{
		StreamWriter sw = new StreamWriter(sourceFile, false);
		IndentedTextWriter tw = new IndentedTextWriter(sw, "    ");
		provider.GenerateCodeFromCompileUnit(cu, tw,
				new CodeGeneratorOptions());
		tw.Close();
	}


	public static void Main(string[] args)
	{
		System.Collections.Generic.List<string> extra;
		System.Collections.Generic.List<string> skip = new System.Collections.Generic.List<string> ();
		bool show_help = false;
		string outputDir = Directory.GetCurrentDirectory();

		// Args processing
		var p = new OptionSet () {
			{ "s|skip=", "the {NAME} of the items to skip.",
					v => skip.Add (v) },
			{ "o|output=", "the {OUTPUT DIR} to use for the generated files.",
					v => outputDir = v },
			{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
		};

		try {
			extra = p.Parse (args);
		} catch (OptionException e) {
			Console.Write ("ender2sharp: ");
			Console.WriteLine (e.Message);
			Console.WriteLine ("Try `ender2sharp --help' for more information.");
			return;
		}

		if (extra.Count == 0)
			show_help = true;

		if (show_help)
		{
			Console.WriteLine ("Usage: ender2sharp.exe [OPTION] LIBNAME");
			Console.WriteLine ("Where OPTION can be the following:");
			Console.WriteLine ("-o|--output The output directory to write the generated files");
			Console.WriteLine ("-s|--skip   Ender item to skip, you can provide as many skip items as you want");
			Console.WriteLine ("-h|--help   Show this help");
			return;
		}

		// Initialize ender before we do anything
		Ender.Main.Init();
		// Find the ender library
		Lib lib = Lib.Find(extra[0]);
		if (lib == null)
		{
			Console.WriteLine ("Library '" + extra[0] + "' not found");
			return;
		}

		// Generate the code with the C# code provider.
		CSharpCodeProvider provider = new CSharpCodeProvider();
		Generator eg = new Generator(lib, provider, skip);
		CodeCompileUnit cu = eg.Generate();

		// Generate the source file
		string sourceFile;
		if (provider.FileExtension[0] == '.')
			sourceFile = extra[0] + "-sharp" + provider.FileExtension;
		else
			sourceFile = extra[0] + "-sharp" + "." + provider.FileExtension;
		GenerateCode(provider, cu, Path.Combine(outputDir, sourceFile));

		// Generate the pc file
		string pcFile = extra[0] + "-sharp.pc";
		GeneratePcFile(lib, Path.Combine(outputDir, pcFile));

		Ender.Main.Shutdown();
	}
}
