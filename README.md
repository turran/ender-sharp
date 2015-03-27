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

The files generated will be at the `TARGET_DIR`

```bash
ls TARGET_DIR
LIB_NAME-sharp.cs
LIB_NAME-sharp.pc
```

What gets binds for?
====================

Objects
-------
```xml
<object name="enesim.renderer.background" inherits="enesim.renderer">
  <prop name="color">
    <setter>
      <arg name="color" type="enesim.color" direction="in" transfer="full"
          nullable="false"/>
    </setter>
    <getter>
      <return type="enesim.color" transfer="full" nullable="false"/>
    </getter>
  </prop>
  <ctor name="new"/>
</object>
```

```cs
public class Background : Renderer {
            
    [DllImport("enesim.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern IntPtr enesim_renderer_background_new();
    [DllImport("enesim.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern uint enesim_renderer_background_color_get(IntPtr self);
    [DllImport("enesim.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern void enesim_renderer_background_color_set(IntPtr self,
        uint color);
            
    protected internal Background(System.IntPtr i, bool owned) : 
            base(i, owned) {
        Initialize(i, owned);
    }
    
    public Background() {
        System.IntPtr ret = enesim_renderer_background_new();
        Initialize(ret, false);
    }
    
    public Enesim.Color Color {
        get {
            uint ret = enesim_renderer_background_color_get(raw);
            return new Color(ret);
        }
        set {
            Enesim.Color color;
            color = value;
            enesim_renderer_background_color_set(raw, color);
        }
    }
}
```

Defs
----
```xml
<def name="enesim.color" type="uint32">
  <function name="components_from">
    <return type="enesim.color" transfer="full" nullable="false"/>
    <arg name="a" type="uint8" direction="in" transfer="full" nullable="false"/>
    <arg name="r" type="uint8" direction="in" transfer="full" nullable="false"/>
    <arg name="g" type="uint8" direction="in" transfer="full" nullable="false"/>
    <arg name="b" type="uint8" direction="in" transfer="full" nullable="false"/>
  </function>
</def>
```

```cs
public class Color {
        
    protected uint value;
        
    [DllImport("enesim.dll", CallingConvention=CallingConvention.Cdecl)]
    private static extern void enesim_color_components_to(uint color,
        out byte a, out byte r, out byte g, out byte b);
        
    public Color(uint v) {
        value = v;
    }
    
    public uint Value {
        get {
            return this.value;
        }
    }
    
    public static   implicit operator Color(uint v) {
        return new Color(v);
    }
    
    public static   implicit operator uint(Color v) {
        return v.value;
    }
    
    public static Enesim.Color ComponentsFrom(byte a, byte r, byte g, byte b) {
        uint ret = enesim_color_components_from(a, r, g, b);
        return new Color(ret);
    }
}
```

Enums
-----
```xml
<enum name="enesim.repeat_mode">
  <value name="restrict"/>
  <value name="pad"/>
  <value name="reflect"/>
  <value name="repeat"/>
</enum>
```

```cs
public enum RepeatModeEnum {
    Restrict,
    Pad,
    Reflect,
    Repeat,
}
```

