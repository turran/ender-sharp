lib_BIN = $(PACKAGE).dll
lib_SRC = \
$(top_srcdir)/src/lib/Basic.cs \
$(top_srcdir)/src/lib/Arg.cs \
$(top_srcdir)/src/lib/Attr.cs \
$(top_srcdir)/src/lib/Constant.cs \
$(top_srcdir)/src/lib/Def.cs \
$(top_srcdir)/src/lib/Enum.cs \
$(top_srcdir)/src/lib/Function.cs \
$(top_srcdir)/src/lib/Item.cs \
$(top_srcdir)/src/lib/Lib.cs \
$(top_srcdir)/src/lib/List.cs \
$(top_srcdir)/src/lib/Main.cs \
$(top_srcdir)/src/lib/Object.cs \
$(top_srcdir)/src/lib/Struct.cs \
$(top_srcdir)/src/lib/Utils.cs \
$(top_srcdir)/src/lib/Value.cs \
$(top_srcdir)/src/lib/Generator.cs

noinst_DATA += $(lib_BIN)

$(lib_BIN): $(lib_SRC) $(output_DIR)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(lib_SRC) \
		-out:$(output_DIR)/$(lib_BIN)

install-data-local:
	echo "$(GACUTIL) /i $(lib_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(output_DIR)/$(lib_BIN) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(lib_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(output_DIR)/$(lib_BIN) $(GACUTIL_FLAGS) || exit 1;
