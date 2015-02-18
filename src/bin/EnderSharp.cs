using Ender;

public class EnderSharp
{
	private static void help()
	{
	}

	public static void Main(string[] args)
	{
		/* do the args processing */
		Generator eg = new Generator();
		eg.generate(args[0]);
	}
}
