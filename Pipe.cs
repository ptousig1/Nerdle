using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Patrick;

namespace Nerdle
	{
	public class Pipe<T> : IEnumerable<T>
		{
		private Lock m_lock = new Lock();
		private bool m_ending = false;
		private int m_maxItems = int.MaxValue;
		private Queue<ManualResetEvent> m_writers = new Queue<ManualResetEvent>();
		private Queue<T> m_items = new Queue<T>();
		private Queue<WaitingReader> m_readers = new Queue<WaitingReader>();

		private class WaitingReader
			{
			internal bool m_success;
			internal T m_item;
			internal ManualResetEvent m_mre;
			}

		public IEnumerator<T> GetEnumerator()
			{
			return new PipeEnumerator<T>(this);
			}

		IEnumerator IEnumerable.GetEnumerator()
			{
			return GetEnumerator();
			}

		public bool TryEnqueue(T a_item)
			{
			ManualResetEvent mreWait = null;
			using(m_lock.WriteLock())
				{
				if (m_ending)
					return false;

				while(m_readers.Count > 0 && m_items.Count > 0)
					{
					WaitingReader wr = m_readers.Dequeue();
					wr.m_success = true;
					wr.m_item = m_items.Dequeue();
					wr.m_mre.Set();
					}

				if (m_writers.Count == 0 && m_items.Count == 0 && m_readers.Count > 0)
					{
					WaitingReader wr = m_readers.Dequeue();
					wr.m_success = true;
					wr.m_item = a_item;
					wr.m_mre.Set();
					}
				else if (m_writers.Count == 0 && m_items.Count < m_maxItems)
					{
					m_items.Enqueue(a_item);
					}
				else
					{
					mreWait = new ManualResetEvent(false);
					m_writers.Enqueue(mreWait);
					}
				}

			if (mreWait != null)
				{
				ThreadEx.Wait(mreWait);		// Must wait outside of m_lock

				using(m_lock.WriteLock())
					{
					if (m_ending)
						return false;

					m_items.Enqueue(a_item);
					}
				}

			return true;
			}

		public bool TryDequeue(out T o_item)
			{
			WaitingReader wr = null;
			using(m_lock.WriteLock())
				{
				if (m_items.Count > 0)
					{
					o_item = m_items.Dequeue();
					return true;
					}
				else
					{
					wr = m_readers.Dequeue();
					wr.m_mre = new ManualResetEvent(false);
					m_readers.Enqueue(wr);
					}
				}

			if (wr != null)
				{
				ThreadEx.Wait(wr.m_mre);		// Must wait outside of m_lock

				o_item = wr.m_item;
				return wr.m_success;
				}

			o_item = default(T);
			return false;
			}

		public void Close()
			{
			using(m_lock.WriteLock())
				{
				m_ending = true;
				}
			}

		public void Abort()
			{
			using(m_lock.WriteLock())
				{
				m_ending = true;

				foreach(WaitingReader wr in m_readers)
					{
					wr.m_success = false;
					wr.m_mre.Set();
					}

				foreach(ManualResetEvent mre in m_writers)
					{
					mre.Set();
					}

				m_readers.Clear();
				m_items.Clear();
				m_writers.Clear();
				}
			}
		}

	public class PipeEnumerator<T> : IEnumerator<T>
		{
		private Pipe<T> m_pipe;
		private T m_current;

		public PipeEnumerator(Pipe<T> a_pipe)
			{
			m_pipe = a_pipe;
			}

		public T Current
			{
			get { return m_current; }
			}

		object IEnumerator.Current
			{
			get { return m_current; }
			}

		public bool MoveNext()
			{
			bool success = m_pipe.TryDequeue(out m_current);
			return success;
			}

		public void Reset()
			{
			throw new NotImplementedException();
			}

		public void Dispose()
			{
			m_pipe = null;
			}
		}
	}
