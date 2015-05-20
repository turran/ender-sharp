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

$(lib_BIN): $(lib_SRC) $(output_DIR)
	$(CSC) -nowarn:169 -unsafe -target:library -debug $(lib_SRC) \
		-out:$(output_DIR)/$(lib_BIN)

enderlibdir = $(libdir)/ender-sharp
enderlib_DATA = $(output_DIR)/$(lib_BIN)
