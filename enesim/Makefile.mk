pkgconfig_DATA += enesim/enesim-sharp.pc

EXTRA_DIST += enesim/enesim-sharp.cs

enesim-sharp.cs:
	$(output_DIR)/ender2sharp.exe -o $(top_srcdir)/enesim enesim

enesim_BIN = enesim-sharp.dll
enesim_SRC = $(top_srcdir)/enesim/enesim-sharp.cs

noinst_DATA += $(enesim_BIN)

$(enesim_BIN): $(eina_BIN) $(enesim_SRC) $(output_DIR)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(enesim_SRC) \
		-out:$(output_DIR)/$(enesim_BIN)
