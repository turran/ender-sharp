pkgconfig_DATA += enesim/enesim-sharp.pc

EXTRA_DIST += enesim/enesim-sharp.cs

enesim-sharp.cs:
	$(output_DIR)/ender2sharp.exe -s enesim.log.add_parametric \
	-s enesim.image.provider -s enesim.image.finder -s enesim.image.provider.descriptor \
	-s enesim.image.finder.descriptor -o $(top_srcdir)/enesim enesim

enesim_BIN = enesim-sharp.dll
enesim_SRC = $(top_srcdir)/enesim/enesim-sharp.cs

enesimlibdir = $(libdir)/enesim-sharp
enesimlib_DATA = $(output_DIR)/$(enesim_BIN)

$(output_DIR)/$(enesim_BIN): $(output_DIR)/$(eina_BIN) $(enesim_SRC) $(output_CHECK)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(enesim_SRC) \
		-r:$(output_DIR)/eina-sharp.dll -r:$(output_DIR)/ender-sharp.dll \
		-out:$(output_DIR)/$(enesim_BIN) -keyfile:$(top_srcdir)/enesim/enesim-sharp.snk
