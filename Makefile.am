AUTOMAKE_OPTIONS = subdir-objects
ACLOCAL_AMFLAGS = -I m4 -I common/m4

MAINTAINERCLEANFILES = \
Makefile.in \
aclocal.m4 \
build-stamp \
compile \
config.guess \
config.h \
config.h.in \
config.h.in~ \
config.sub \
configure \
configure-stamp \
depcomp \
install-sh \
ltconfig \
ltmain.sh \
missing \
mkinstalldirs \
stamp-h.in \
$(PACKAGE)-$(VERSION).tar.bz2 \
$(PACKAGE)-$(VERSION).tar.gz \
m4/libtool.m4 \
m4/lt~obsolete.m4 \
m4/ltoptions.m4 \
m4/ltsugar.m4 \
m4/ltversion.m4

output_DIR = $(top_builddir)/bin
lib_LTLIBRARIES =
check_PROGRAMS =
EXTRA_DIST =
noinst_DATA =

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA =

### Library

include src/lib/Makefile.mk

### Binary

include src/bin/Makefile.mk

### The generated bindings
include eina/Makefile.mk
include enesim/Makefile.mk

EXTRA_DIST += \
AUTHORS \
COPYING \
README.md \
ender-sharp.pc.in \
m4/efl_dlfcn.m4 \
m4/ens_doxygen.m4

sed_process = \
$(SED) \
-e 's,@VERSION\@,$(VERSION),g' \
-e 's,@VMAJ\@,$(VMAJ),g' \
-e 's,@prefix\@,$(prefix),g' \
-e 's,@exec_prefix\@,$(exec_prefix),g' \
-e 's,@libdir\@,$(libdir),g' \
-e 's,@includedir\@,$(includedir),g' \
-e 's,@pkgincludedir\@,$(pkgincludedir),g' \
-e 's,@pkglibdir\@,$(pkglibdir),g' \
-e 's,@ender_sharp_requirements_private\@,$(ender_sharp_requirements_private),g' \
-e 's,@ender_sharp_requirements\@,$(ender_sharp_requirements),g' \
< $< > $@ || rm $@

pc_verbose = $(pc_verbose_@AM_V@)
pc_verbose_ = $(pc_verbose_@AM_DEFAULT_V@)
pc_verbose_0 = @echo "  PC      " $@;

%.pc: %.pc.in Makefile
	@rm -f $@
	$(pc_verbose)$(sed_process)


pkgconfig_DATA += ender-sharp.pc

$(output_DIR):
	@mkdir -p $(output_DIR)
