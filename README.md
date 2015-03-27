What is it?
===========
Ender-Sharp lets you create automatic bindings for C# for any library described with [Ender](https://github.com/turran/ender).

Dependencies
============
+ [Ender](https://github.com/turran/ender)
+ [Mono](http://www.mono-project.com/)

Building and Installation
=========================
```bash
./configure
make
make install
```

How it works?
=============
First you need to have a [XML Ender file](https://github.com/turran/ender/wiki/XML-File-Format) for your library.
Once you have one, the ender2sharp binary will create a .NET source file and a pkg-config .pc file with the bindings of your lib.

```bash
./ender2sharp.exe -o TARGET_DIR LIB_NAME
```
