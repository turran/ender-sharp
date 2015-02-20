using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace Ender
{
	/* our match to an Eina List */
	public class List : IEnumerable, IDisposable
	{
		[DllImport("libeina.dll")]
		static extern void eina_list_free(IntPtr i);
		[DllImport("libeina.dll")]
		static extern IntPtr eina_list_nth(IntPtr i, uint n);

		[StructLayout(LayoutKind.Sequential)]
		private struct EinaListAccounting
		{
			public IntPtr last;
			public uint count;
			// magic stuff
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct EinaList
		{
			public IntPtr data;
			public IntPtr next;
			public IntPtr prev;
			public IntPtr accounting;
			// magic stuff
		}

		private IntPtr raw;
		private System.Type element_type;
		private bool owned;
		private bool elements_owned;
		// Track whether Dispose has been called.
		private bool disposed = false;

		#region Constructors
		public List(IntPtr l, System.Type element_type, bool owned, bool elements_owned)
		{
			raw = l;
			this.element_type = element_type;
			this.owned = owned;
			this.elements_owned = elements_owned;
		}

		public List(IntPtr l, System.Type element_type, bool owned) : this(l, element_type, owned, false)
		{
		}

		public List(IntPtr l, System.Type element_type) : this(l, element_type, false)
		{
		}

		public List(IntPtr l) : this(l, null)
		{
		}

		private List()
		{
		}

		~List()
		{
			Dispose(false);
		}
		#endregion

		#region IDisposable
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposed)
			{
				// Dispose managed data
				// Dispose unmanaged data
				// Empty the list
				Empty();
				// Free it
				Free();
				disposed = true;
			}
		}
		#endregion

		public IntPtr Handle
		{
			get {
				return raw;
			}
		}

		public object Nth(uint index)
		{
			IntPtr data = eina_list_nth(raw, index);
			object ret = MarshalData(data, element_type);
			return ret;
		}

		public uint Count
		{
			get {
				EinaList l = MarshalList(raw);
				EinaListAccounting acc = MarshalAccounting(l.accounting);
				return acc.count;
			}
		}

		private void Empty()
		{
			if (raw == IntPtr.Zero)
				return;

			// Free every element
			if (elements_owned)
			{
				EinaList l = MarshalList(raw); 
				for (uint i = 0; i < Count; i++)
				{
					// Free the data
					FreeData(l.data, element_type);
					if (l.next != IntPtr.Zero)
						l = MarshalList(l.next);
				}
			} 
		}

		private void Free()
		{
			if (owned)
				eina_list_free(raw);
			raw = IntPtr.Zero;		
		}

		#region IEnumerable implementation
		public IEnumerator GetEnumerator()
		{
			return new ListEnumerator(this);
		}

		private class ListEnumerator : IEnumerator
		{
			private IntPtr current = IntPtr.Zero;
			private List list;

			public ListEnumerator (List list)
			{
				this.list = list;
			}

			public object Current {
				get {
					EinaList l = MarshalList(current);
					object ret = null;
					ret = MarshalData(l.data, list.element_type);
					return ret;
				}
			}

			public bool MoveNext ()
			{
				if (current == IntPtr.Zero)
					current = list.Handle;
				else
				{
					EinaList l = MarshalList(current);
					current = l.next;
				}
				return (current != IntPtr.Zero);
			}

			public void Reset ()
			{
				current = IntPtr.Zero;
			}
		}
		#endregion

		#region Marshaling
		private static EinaList MarshalList(IntPtr l)
		{
			EinaList list = (EinaList)Marshal.PtrToStructure(l, typeof(EinaList));
			return list;
		}

		private static EinaListAccounting MarshalAccounting(IntPtr l)
		{
			EinaListAccounting acc = (EinaListAccounting)Marshal.PtrToStructure(l, typeof(EinaListAccounting));
			return acc;
		}

		private static object MarshalData(IntPtr data, System.Type type)
		{
			object ret = null;
			if (type == typeof (string))
				ret = Marshal.PtrToStringAnsi(data);
			else if (type == typeof(IntPtr))
				ret = data;
			else if (type.IsValueType)
				ret = Marshal.PtrToStructure(data, type);
			// We assume that there will exist a constructor that receives the IntPtr
			// and the owned param in order to create objects
			else
				ret = Activator.CreateInstance(type, new object[] {data, false});
			return ret;
		}

		private static void FreeData(IntPtr data, System.Type type)
		{
			// Given that ender objects do not inherit from a common object
			// like GObject we check if the type implements the disposable
			// case, for that case we create a new object without owning the
			// pointer and dispose (internally it will free, unref or whatever)
			if (typeof(IDisposable).IsAssignableFrom(type))
			{
				object o = Activator.CreateInstance(type, new object[] {data, true});
				((IDisposable)o).Dispose();
			}
			else
				Marshal.FreeHGlobal(data);
		}
		#endregion
	}
}
