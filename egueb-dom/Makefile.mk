pkgconfig_DATA += egueb-dom/egueb-dom-sharp.pc

EXTRA_DIST += egueb-dom/egueb-dom-sharp.cs

egueb-dom-sharp.cs:
	$(output_DIR)/ender2sharp.exe -s egueb.dom.attr.get -s egueb.dom.attr.set \
	-s egueb.dom.attr.final_get -s egueb.dom.attr.final_get_va \
	-s egueb.dom.element.attribute_masked_set -s egueb.dom.element.attribute_masked_get \
	-s egueb.dom.string.new_with_static_string -s egueb.dom.string.steal \
	-o $(top_srcdir)/egueb-dom egueb-dom

egueb_dom_BIN = egueb-dom-sharp.dll
egueb_dom_SRC = $(top_srcdir)/egueb-dom/egueb-dom-sharp.cs

egueb_domlibdir = $(libdir)/egueb-dom-sharp
egueb_domlib_DATA = $(output_DIR)/$(egueb_dom_BIN)

$(output_DIR)/$(egueb_dom_BIN): $(output_DIR)/$(lib_BIN) $(output_DIR)/$(enesim_BIN) $(output_DIR)/$(eina_BIN) $(egueb_dom_SRC) $(output_CHECK)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(egueb_dom_SRC) \
		-r:$(output_DIR)/eina-sharp.dll -r:$(output_DIR)/enesim-sharp.dll -r:$(output_DIR)/ender-sharp.dll \
		-out:$(output_DIR)/$(egueb_dom_BIN) -keyfile:$(top_srcdir)/egueb-dom/egueb-dom-sharp.snk
