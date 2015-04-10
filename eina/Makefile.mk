pkgconfig_DATA += eina/eina-sharp.pc

EXTRA_DIST += eina/eina-sharp.cs

eina-sharp.cs:
	$(output_DIR)/ender2sharp.exe -o $(top_srcdir)/eina eina
