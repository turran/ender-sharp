#ifndef _EGUEB_DOM_GLUE_H
#define _EGUEB_DOM_GLUE_H

#include "Egueb_Dom.h"

#ifdef EAPI
# undef EAPI
#endif

#ifdef _WIN32
# ifdef EGUEB_DOM_GLUE_BUILD
#  ifdef DLL_EXPORT
#   define EAPI __declspec(dllexport)
#  else
#   define EAPI
#  endif
# else
#  define EAPI __declspec(dllimport)
# endif
#else
# ifdef __GNUC__
#  if __GNUC__ >= 4
#   define EAPI __attribute__ ((visibility("default")))
#  else
#   define EAPI
#  endif
# else
#  define EAPI
# endif
#endif

EAPI Egueb_Dom_String * egueb_dom_window_feature_window_name_get(void);
EAPI Egueb_Dom_String * egueb_dom_event_key_down_get(void);
EAPI Egueb_Dom_String * egueb_dom_event_key_up_get(void);

#endif
