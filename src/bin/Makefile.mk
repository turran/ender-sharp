bin_BIN = ender2sharp.exe
bin_SRC = \
$(top_srcdir)/src/bin/EnderSharp.cs \
$(top_srcdir)/src/bin/Options.cs

src/bin/Options.cs:
	cp `pkg-config --variable=Sources mono-options` $(top_srcdir)/src/bin/

$(output_DIR)/$(bin_BIN): $(bin_SRC) $(output_CHECK)
	$(CSC) -nowarn:169 -unsafe -target:exe $(bin_SRC) \
		-out:$(output_DIR)/$(bin_BIN) -r:$(output_DIR)/$(PACKAGE).dll

enderbindir = $(bindir)
enderbin_DATA = $(output_DIR)/$(bin_BIN)
