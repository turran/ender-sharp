bin_BIN = ender2sharp.exe
bin_SRC = \
src/bin/EnderSharp.cs \
src/bin/Options.cs

noinst_DATA += $(bin_BIN)

src/bin/Options.cs:
	cp `pkg-config --variable=Sources mono-options` $(top_srcdir)/src/bin/

$(bin_BIN): $(bin_SRC) $(output_DIR)
	$(CSC) -nowarn:169 -unsafe -target:exe $(bin_SRC) \
		-out:$(output_DIR)/$(bin_BIN) -r:$(output_DIR)/$(PACKAGE).dll
