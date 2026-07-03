using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Patrick;

namespace Nerdle
	{
	public class Progress : IDisposable
		{
		private Progress m_parent;
		private string m_label;
		private string m_info;
		private int m_current;
		private int m_total;

		static private ThreadLocal<Progress> s_tail = new ThreadLocal<Progress>();
		static private ThreadLocal<DateTime> s_nextPrint = new ThreadLocal<DateTime>();
//		static private Progress s_tail;
//		static private DateTime s_nextPrint = DateTime.MinValue;

		public Progress(string a_label, int a_total)
			{
			m_label = a_label;
			m_total = a_total;
			m_parent = s_tail.Value;
			s_tail.Value = this;
			s_nextPrint.Value = DateTime.MinValue;
			}

		public void Dispose()
			{
			DebugEx.Assert(s_tail.Value == this);
			s_tail.Value = s_tail.Value.m_parent;
			}

		public int Current
			{
			get { return m_current; }
			set { m_current = value; PeriodicTrace(); }
			}

		public string Label		{ get { return m_label; } set { m_label = value; } }
		public string Info		{ get { return m_info; } set { m_info = value; } }

		public void Increment()
			{
			m_current++;
			PeriodicTrace();
			}

		private void AppendPart(StringBuilder a_sb)
			{
			if (m_parent != null)
				m_parent.AppendPart(a_sb);
			if (m_info == null)
				a_sb.AppendFormat("{0} ({1} of {2}),\t", m_label, m_current, m_total);
			else
				a_sb.AppendFormat("{0} ({1} of {2}) {3},\t", m_label, m_current, m_total, m_info);
			}

		static public void PeriodicTrace()
			{
			if (DateTime.UtcNow > s_nextPrint.Value)
				{
				StringBuilder sb = new StringBuilder();
				sb.Append("Progress: ");
				s_tail.Value.AppendPart(sb);
				MyTrace.WriteLine(sb.ToString());
				s_nextPrint.Value = DateTime.UtcNow.AddSeconds(1);
				}
			}
		}
	}
