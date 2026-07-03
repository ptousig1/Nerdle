using System;
using System.Collections.Generic;
using System.Text;
using Patrick;

namespace Patrick
	{
	public class Fraction
		{
		private long m_numerator;
		private long m_denominator;

		static public Fraction MinValue = new Fraction(long.MinValue, 1);
		static public Fraction MaxValue = new Fraction(long.MaxValue, 1);

		public Fraction()
			{
			m_numerator = 0;
			m_denominator = 1;
			}

		public Fraction(int a_integer)
			{
			m_numerator = a_integer;
			m_denominator = 1;
			}

		public Fraction(long a_numerator, long a_denominator)
			{
			if (a_denominator <= 0)
				throw new NotFiniteNumberException();

			m_numerator = a_numerator;
			m_denominator = a_denominator;

			long gcd = MathEx.GCD(m_numerator, m_denominator);
			if (gcd > 1)
				{
				m_numerator = m_numerator / gcd;
				m_denominator = m_denominator / gcd;
				}
			}

		public override string ToString()
			{
			if (m_denominator == 0)
				return "NaN";
			else if (m_denominator == 1)
				return m_numerator.ToString();
			else
				return String.Format("{0}/{1}", m_numerator, m_denominator);
			}

		public string ToDecimalString()
			{
			return ((decimal) this).ToString();
			}

		static public Fraction NaN = new Fraction() { m_numerator = 0, m_denominator = 0 };

		static public implicit operator Fraction(int a)
			{
			return new Fraction(a, 1);
			}

		static public implicit operator decimal(Fraction a)
			{
			if (a.m_denominator == 0)
				throw new NotFiniteNumberException();
			return (decimal) a.m_numerator / a.m_denominator;
			}

		static public Fraction operator +(Fraction a, Fraction b)
			{
			if (a.m_denominator == b.m_denominator)
				return new Fraction(a.m_numerator + b.m_numerator, a.m_denominator);

			return new Fraction(a.m_numerator * b.m_denominator + b.m_numerator * a.m_denominator, a.m_denominator * b.m_denominator);
			}

		static public Fraction operator -(Fraction a, Fraction b)
			{
			if (a.m_denominator == b.m_denominator)
				return new Fraction(a.m_numerator - b.m_numerator, a.m_denominator);

			return new Fraction(a.m_numerator * b.m_denominator - b.m_numerator * a.m_denominator, a.m_denominator * b.m_denominator);
			}

		static public Fraction operator *(Fraction a, Fraction b)
			{
			return new Fraction(a.m_numerator * b.m_numerator, a.m_denominator * b.m_denominator);
			}

		static public Fraction operator /(Fraction a, Fraction b)
			{
			return new Fraction(a.m_numerator * b.m_denominator, a.m_denominator * b.m_numerator);
			}

		static public bool operator <(Fraction a, Fraction b)
			{
			return Compare(a, b) < 0;
			}

		static public bool operator >(Fraction a, Fraction b)
			{
			return Compare(a, b) > 0;
			}

		static public bool operator ==(Fraction a, Fraction b)
			{
			return Compare(a, b) == 0;
			}

		static public bool operator !=(Fraction a, Fraction b)
			{
			return Compare(a, b) != 0;
			}

		static public int Compare(Fraction a, Fraction b)
			{
			if (a is null || b is null)
				{
				// A null is considered smaller than a non-null
				if (a is null && b is null)
					return 0;
				if (a is null)
					return -1;
				return 1;
				}

			if (a.m_denominator == b.m_denominator)
				return LongEx.Compare(a.m_numerator, b.m_numerator);

			long aWhole = a.m_numerator / a.m_denominator;
			long bWhole = b.m_numerator / b.m_denominator;
			int cmp = LongEx.Compare(aWhole, bWhole);
			if (cmp != 0)
				return cmp;

			double aFrac = (double) (a.m_numerator - (aWhole * a.m_denominator)) / a.m_denominator;
			double bFrac = (double) (b.m_numerator - (bWhole * b.m_denominator)) / b.m_denominator;
			cmp = DoubleEx.Compare(aFrac, bFrac);
			if (cmp != 0)
				return cmp;

			DebugEx.SelfHalt();
			throw new InconceivableException();
//			long aNumerator = a.m_numerator * b.m_denominator;
//			long bNumerator = b.m_numerator * a.m_denominator;
//			DebugEx.Assert(aNumerator != bNumerator);
//			return LongEx.Compare(aNumerator, bNumerator);
			}

		public override bool Equals(object obj)
			{
			Fraction that = obj as Fraction;
			if (that is null)
				return false;
			return this == that;
			}

		public override int GetHashCode()
			{
			int hash = m_numerator.GetHashCode() ^ m_denominator.GetHashCode();
			return hash;
			}

		static public Fraction Parse(string a_string)
			{
			long num = 0;
			long den = 0;
			if (a_string.TryMatch("^([-0-9]+)/([0-9]+)$", ref num, ref den))
				return new Fraction(num, den);
			throw new ArgumentException();
			}
		}
	}
