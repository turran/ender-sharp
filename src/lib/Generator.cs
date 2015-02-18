using System;
using System.CodeDom;
using System.Runtime.InteropServices;

namespace Ender
{
	public class Generator
	{
		/* ender_main.h */
		[DllImport("libender.dll")]
		static extern void ender_init();
		[DllImport("libender.dll")]
		static extern void ender_shutdown();
		/* ender_lib.h */
		//static extern const Ender_Lib * ender_lib_find(const char *name);
		//static extern int ender_lib_version_get(const Ender_Lib *thiz);
		//static extern const char * ender_lib_name_get(const Ender_Lib *thiz);
		//static extern Eina_List * ender_lib_dependencies_get(const Ender_Lib *thiz);
		//static extern Ender_Item * ender_lib_item_find(const Ender_Lib *thiz, const char *name);
		//static extern Eina_List * ender_lib_item_list(const Ender_Lib *thiz, Ender_Item_Type type);
		/* ender_item.h */
		//static extern Ender_Item * ender_item_ref(Ender_Item *thiz);
		//static extern void ender_item_unref(Ender_Item *thiz);
		//static extern const char * ender_item_name_get(Ender_Item *thiz);
		//static extern Ender_Item_Type ender_item_type_get(Ender_Item *thiz);
		//static extern Ender_Item * ender_item_parent_get(Ender_Item *thiz);
		//static extern const char * ender_item_type_name_get(Ender_Item_Type type);
		//static extern const Ender_Lib * ender_item_lib_get(Ender_Item *thiz);
		//static extern Eina_Bool ender_item_is_exception(Ender_Item *i);

		private void generateObject(IntPtr item)
		{

		}

		private void generateItem(IntPtr item)
		{

		}

		public CodeCompileUnit generate(string ns)
		{
			/* get the ender library */
			/* iterate over every object */
			/* jump to root nodes, ie. items that do not have inheritances */
			/* in the mean time add every non root node into a queue */
			/* iterate the childs and remove theme from the queue in case it is already processed */
			/* finally continue with the queue again */
			return null;
		}

		static Generator()
		{
			ender_init();
		}
	}
}
