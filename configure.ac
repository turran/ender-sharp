
### Version

m4_define([v_maj], [0])
m4_define([v_min], [0])
m4_define([v_mic], [1])
m4_define([v_ver], [v_maj.v_min.v_mic])

m4_define([lt_cur], m4_eval(v_maj + v_min))
m4_define([lt_rev], v_mic)
m4_define([lt_age], v_min)

AC_INIT([ender-sharp], [v_ver], [enesim-devel@googlegroups.com])
AC_PREREQ([2.60])
AC_CONFIG_SRCDIR([configure.ac])
AC_CONFIG_MACRO_DIR([m4])
AC_CONFIG_HEADERS([config.h])

AM_INIT_AUTOMAKE([foreign 1.6 dist-bzip2])
m4_ifdef([AM_SILENT_RULES], [AM_SILENT_RULES([yes])])

LT_PREREQ([2.2])
LT_INIT([win32-dll])

version_info="lt_cur:lt_rev:lt_age"
AC_SUBST([version_info])

AC_DEFINE_UNQUOTED([VERSION_MAJOR], [v_maj], [Major version])
AC_DEFINE_UNQUOTED([VERSION_MINOR], [v_min], [Minor version])
AC_DEFINE_UNQUOTED([VERSION_MICRO], [v_mic], [Micro version])

VMAJ=v_maj
AC_SUBST([VMAJ])

## Add parameters for aclocal (keep in sync with Makefile.am)
AC_SUBST(ACLOCAL_AMFLAGS, "-I m4 -I common/m4")

### Additional options to configure

### default values

AC_CANONICAL_HOST

### Checks for programs

# pkg-config
PKG_PROG_PKG_CONFIG

# Check whether pkg-config supports Requires.private
if ! $PKG_CONFIG --atleast-pkgconfig-version 0.22; then
   AC_MSG_ERROR([pkg-config >= 0.22 is required])
fi

LT_SYS_MODULE_EXT

### Checks for libraries
dnl Check for Mono
PKG_CHECK_MODULES(MONO_DEPENDENCY, mono >= 1.0, has_mono=true, has_mono=false)
PKG_CHECK_MODULES(MONO_DEPENDENCY, mint >= 1.0, has_mint=true, has_mint=false)

if test "x$has_mono" = "xtrue"; then
	if test `uname -s` = "Darwin"; then
		AC_PATH_PROG(RUNTIME, mono, no)
		AC_PATH_PROG(CSC, mcs, no)
		LIB_PREFIX=
		LIB_SUFFIX=.dylib
	else
		AC_PATH_PROG(RUNTIME, mono, no)
		AC_PATH_PROG(CSC, mcs, no)
		LIB_PREFIX=.so
		LIB_SUFFIX=
	fi
else
	if test "x$has_mint" = "xtrue"; then
		AC_PATH_PROG(RUNTIME, mint, no)
		AC_PATH_PROG(CSC, mcs, no)
		LIB_PREFIX=.so
		LIB_SUFFIX=
	else
		AC_PATH_PROG(CSC, csc.exe, no)
		if test x$CSC = "xno"; then
			AC_MSG_ERROR([You need to install either mono or .Net])
		else
			RUNTIME=
			LIB_PREFIX=
			LIB_SUFFIX=.dylib
		fi
	fi
fi

CS="C#"
if test "x$CSC" = "xno" ; then
    AC_MSG_ERROR([No $CS compiler found])
fi

AC_PATH_PROG(GACUTIL, gacutil, no)
if test "x$GACUTIL" = "xno" ; then
	AC_MSG_ERROR([No gacutil tool found])
fi

AC_SUBST(RUNTIME)
AC_SUBST(CSC)
AC_SUBST(GACUTIL)

AC_ARG_ENABLE([egueb-dom], [AS_HELP_STRING([--enable-egueb-dom], [Enable Ecore Win32 window])],
  [
    if test "x$enableval" = "xyes" ; then
       want_egueb_dom="yes"
    else
       want_egueb_dom="no"
    fi
  ],
  [want_egueb_dom="yes"]
)
PKG_CHECK_MODULES([EGUEB_DOM], [egueb-dom], [have_egueb_dom="yes"], [have_egueb_dom="no"])
build_egueb_dom="no"

if test "x${have_egueb_dom}" = "xyes" -a "x${want_egueb_dom}" = "xyes"; then
  build_egueb_dom="yes"
fi

if test "x${build_egueb_dom}" = "xyes"; then
  AC_DEFINE([BUILD_EGUEB_DOM], [1], [Have support for egueb_dom])
fi
AM_CONDITIONAL([BUILD_EGUEB_DOM], [test "x${build_egueb_dom}" = "xyes"])

## Make the debug preprocessor configurable

AC_CONFIG_FILES([
Makefile
])

AC_OUTPUT

#####################################################################
## Info

echo
echo
echo
echo "------------------------------------------------------------------------"
echo "$PACKAGE $VERSION"
echo "------------------------------------------------------------------------"
echo
echo
echo "Configuration Options Summary:"
echo
echo "Bindings...............:"
echo "  ender................: yes"
echo "  enesim...............: yes"
echo "  egueb-dom............: ${build_egueb_dom}"
echo "  egueb-svg............: yes"
echo
echo "Compilation............: make (or gmake)"
echo "  compiler.............: $CSC"
echo
echo "Installation...........: make install (as root if needed, with 'su' or 'sudo')"
echo "  prefix...............: $prefix"
echo
