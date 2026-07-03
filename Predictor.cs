using System;
using System.Collections.Generic;
using System.Text;
using Patrick;

namespace Nerdle
	{
	internal class Predictor
		{
		const int c_arraySize = 16384;
		private int m_topSize = 0;
		private int[] m_totals = new int[c_arraySize];
		private int[] m_counts = new int[c_arraySize];
		private double[] m_calcs = new double[c_arraySize];

		public Predictor()
			{
			for(int i=0; i<m_calcs.Length; i++)
				m_calcs[i] = double.NaN;
			}

		public void Add(int a_listSize, int a_gt3)
			{
			m_counts[a_listSize]++;
			m_totals[a_listSize] += a_gt3;
			if (a_listSize > m_topSize)
				m_topSize = a_listSize;
			m_calcs[a_listSize] = (double) m_totals[a_listSize] / (double) m_counts[a_listSize];
			}

		public double Get(int a_listSize)
			{
			if (double.IsNaN(m_calcs[a_listSize]))
				{
				if (a_listSize > m_topSize)
					return Get(m_topSize);

				int a = a_listSize - 1;
				while(a > 0 && m_counts[a] == 0)
					a--;
				int b = a_listSize + 1;
				while(m_counts[b] == 0)
					b++;
				double ya = (double) m_totals[a] / (double) m_counts[a];
				double yb = (double) m_totals[b] / (double) m_counts[b];
				double yd = yb - ya;
				double xr = (double) (a_listSize - a) / (double) (b - a);
				double yp = ya + (xr * yd);
				m_calcs[a_listSize] = yp;
				return yp;
				}
			return m_calcs[a_listSize];
			}
		}
	}
