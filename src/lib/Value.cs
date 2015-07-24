using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	public enum ValueType
	{
		BOOL,
		UINT8,
		INT8,
		UINT32,
		INT32,
		UINT64,
		INT64,
		DOUBLE,
		STRING,
		POINTER,
		SIZE,
	}

	public class Value
	{
		private ValueStruct rawStruct;

		public Value(IntPtr i, bool owned)
		{
            		rawStruct = ((ValueStruct)(Marshal.PtrToStructure(i, typeof(ValueStruct))));
			if (owned)
			{
				DestroyRaw(i);
			}
		}

		public bool B {
			get {
				if (rawStruct.b > 0)
					return true;
				else
					return false;
			}
		}

		public sbyte I8 {
			get {
				return rawStruct.i8;
			}
		}

		public byte U8 {
			get {
				return rawStruct.u8;
			}
		}

		public int I32 {
			get {
				return rawStruct.i32;
			}
		}

		public uint U32 {
			get {
				return rawStruct.u32;
			}
		}

		public long I64 {
			get {
				return rawStruct.i64;
			}
		}

		public ulong U64 {
			get {
				return rawStruct.u64;
			}
		}

		public double D {
			get {
				return rawStruct.d;
			}
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct ValueStruct
		{
			[FieldOffset(0)]
			public byte b;
			[FieldOffset(0)]
			public sbyte i8;
			[FieldOffset(0)]
			public byte u8;
			[FieldOffset(0)]
			public int i32;
			[FieldOffset(0)]
			public uint u32;
			[FieldOffset(0)]
			public long i64;
			[FieldOffset(0)]
			public ulong u64;
			[FieldOffset(0)]
			public double d;
			[FieldOffset(0)]
			public IntPtr ptr;
		}

		public static System.IntPtr CreateRaw() {
			System.IntPtr raw;
			raw = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(ValueStruct)));
			return raw;
		}

		public static void DestroyRaw(System.IntPtr raw) {
			Marshal.FreeHGlobal(raw);
		}
	}
}
