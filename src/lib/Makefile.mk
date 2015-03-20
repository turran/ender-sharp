lib_BIN = $(PACKAGE).dll
lib_SRC = \
src/lib/Basic.cs \
src/lib/Arg.cs \
src/lib/Attr.cs \
src/lib/Constant.cs \
src/lib/Def.cs \
src/lib/Enum.cs \
src/lib/Function.cs \
src/lib/Item.cs \
src/lib/Lib.cs \
src/lib/List.cs \
src/lib/Main.cs \
src/lib/Object.cs \
src/lib/Struct.cs \
src/lib/Utils.cs \
src/lib/Value.cs \
src/lib/Generator.cs

noinst_DATA += $(lib_BIN)

$(lib_BIN): $(lib_SRC)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(lib_SRC) \
		-out:$(lib_BIN)

install-data-local:
	echo "$(GACUTIL) /i $(lib_BIN) /f $(GACUTIL_FLAGS)";  \
        $(GACUTIL) /i $(lib_BIN) /f $(GACUTIL_FLAGS) || exit 1;

uninstall-local:
	echo "$(GACUTIL) /u $(lib_BIN) $(GACUTIL_FLAGS)"; \
        $(GACUTIL) /u $(lib_BIN) $(GACUTIL_FLAGS) || exit 1;
