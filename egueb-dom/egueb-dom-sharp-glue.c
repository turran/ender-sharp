#include "egueb-dom-sharp-glue.h"

EAPI Egueb_Dom_String * egueb_dom_feature_window_name_get(void)
{
	return egueb_dom_string_ref(EGUEB_DOM_FEATURE_WINDOW_NAME);
}

EAPI Egueb_Dom_String * egueb_dom_feature_render_name_get(void)
{
	return egueb_dom_string_ref(EGUEB_DOM_FEATURE_RENDER_NAME);
}

EAPI Egueb_Dom_String * egueb_dom_event_key_down_get(void)
{
	return egueb_dom_string_ref(EGUEB_DOM_EVENT_KEY_DOWN);
}

EAPI Egueb_Dom_String * egueb_dom_event_key_up_get(void)
{
	return egueb_dom_string_ref(EGUEB_DOM_EVENT_KEY_DOWN);
}
