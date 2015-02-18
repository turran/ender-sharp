To use ender dynamically you can do:
using Ender;

public class Test01
{
	public static void Main()
	{
		// use ender to get the class/namespace??
		dynamic rendererClass = Ender.getClass("enesim.renderer");
		// use ender to create a new object based on the ns.type tuple
                dynamic renderer = new EnderObject("enesim.renderer");
		// use the class new() method to create a new object
		dynamic renderer = renderClass.new();
	}
}

To use ender statically you can do:
using Ender;
using System.CodeDom;

public class Test02
{
	public static void Main()
	{
		CompilerParameters cp;
		CodeCompilerUnit cu = Ender.generate("enesim");
		codeprovider.CompileAssemblyFromDom (cp, cu);
		// finally deploy the dll into the system
	}
}

To use the ender -> csharp automatic generator you can do
ender-csharp <namespace> outdir
which will generate:

namespace.dll
namespace-dll.config
namespace-sharp.pc
