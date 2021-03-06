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
output_CHECK = $(output_DIR)/.d

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
include egueb-dom/Makefile.mk
include egueb-svg/Makefile.mk

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

dllconfig_verbose = $(dllconfig_verbose_@AM_V@)
dllconfig_verbose_ = $(dllconfig_verbose_@AM_DEFAULT_V@)
dllconfig_verbose_0 = @echo "  DLLCONF " $@;

%.pc: %.pc.in Makefile
	@rm -f $@
	$(pc_verbose)$(sed_process)

%.dll.config: %.dll.config.in Makefile
	@rm -f $@
	$(dllconfig_verbose)$(sed_process)

pkgconfig_DATA += ender-sharp.pc

$(output_CHECK):
	@mkdir -p $(output_DIR)
	@touch $(output_CHECK)

GACUTIL_FLAGS = /gacdir $(DESTDIR)$(prefix)/lib

install-data-local:
	echo "$(GACUTIL) /i $(output_DIR)/$(lib_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(lib_BIN) /f $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /i $(output_DIR)/$(eina_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(eina_BIN) /f $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /i $(output_DIR)/$(enesim_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(enesim_BIN) /f $(GACUTIL_FLAGS) || exit 1;
if BUILD_EGUEB_DOM
	echo "$(GACUTIL) /i $(output_DIR)/$(egueb_dom_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(egueb_dom_BIN) /f $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /i $(output_DIR)/$(egueb_svg_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(egueb_svg_BIN) /f $(GACUTIL_FLAGS) || exit 1;
endif

uninstall-local:
	echo "$(GACUTIL) /u $(output_DIR)/$(lib_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(lib_BIN) $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /u $(output_DIR)/$(eina_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(eina_BIN) $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /u $(output_DIR)/$(enesim_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(enesim_BIN) $(GACUTIL_FLAGS) || exit 1;
if BUILD_EGUEB_DOM
	echo "$(GACUTIL) /u $(output_DIR)/$(egueb_dom_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(egueb_dom_BIN) $(GACUTIL_FLAGS) || exit 1;
	echo "$(GACUTIL) /u $(output_DIR)/$(egueb_svg_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(egueb_svg_BIN) $(GACUTIL_FLAGS) || exit 1;
endif
