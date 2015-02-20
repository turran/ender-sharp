using System;
using System.Runtime.InteropServices;

namespace Ender
{
	public static class Main
	{
		/* ender_main.h */
		[DllImport("libender.dll")]
		static extern void ender_init();
		[DllImport("libender.dll")]
		static extern void ender_shutdown();

		static public void Init()
		{
			ender_init();
		}

		static public void Shutdown()
		{
			ender_shutdown();
		}
	}
}
