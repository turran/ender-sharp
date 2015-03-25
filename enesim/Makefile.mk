pkgconfig_DATA += enesim/enesim-sharp.pc

EXTRA_DIST += enesim/enesim-sharp.cs

enesim-sharp.cs:
	$(output_DIR)/ender2sharp.exe enesim

