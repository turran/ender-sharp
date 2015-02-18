bin_BIN = ender2sharp.exe
bin_SRC = src/bin/EnderSharp.cs

noinst_DATA += $(bin_BIN)

$(bin_BIN): $(bin_SRC)
	$(CSC) -nowarn:169 -unsafe -target:exe $(bin_SRC) \
		-out:$(bin_BIN) -r:$(top_builddir)/$(PACKAGE).dll
