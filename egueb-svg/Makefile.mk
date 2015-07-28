pkgconfig_DATA += egueb-svg/egueb-svg-sharp.pc

EXTRA_DIST += egueb-svg/egueb-svg-sharp.cs

egueb-svg-sharp.cs:
	$(output_DIR)/ender2sharp.exe -o $(top_srcdir)/egueb-svg egueb-svg

egueb_svg_BIN = egueb-svg-sharp.dll
egueb_svg_SRC = $(top_srcdir)/egueb-svg/egueb-svg-sharp.cs

egueb_svglibdir = $(libdir)/egueb-svg-sharp
egueb_svglib_DATA = $(output_DIR)/$(egueb_svg_BIN)

$(output_DIR)/$(egueb_svg_BIN): $(output_DIR)/$(lib_BIN) $(output_DIR)/$(enesim_BIN) $(output_DIR)/$(eina_BIN) $(output_DIR)/$(egueb_dom_BIN) $(egueb_svg_SRC) $(output_CHECK)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(egueb_svg_SRC) \
		-r:$(output_DIR)/eina-sharp.dll -r:$(output_DIR)/enesim-sharp.dll \
		-r:$(output_DIR)/ender-sharp.dll -r:$(output_DIR)/egueb-dom-sharp.dll \
		-out:$(output_DIR)/$(egueb_svg_BIN) -keyfile:$(top_srcdir)/egueb-svg/egueb-svg-sharp.snk
