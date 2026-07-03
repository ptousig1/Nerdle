using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Patrick;
using Patrick.Csv2;
using Patrick.Json5;
using Patrick.MemoryMap3;

namespace Nerdle
	{
	static internal unsafe class Hint
		{
		static private MmFile s_table;
		static private MmPin s_pin;

		static private ushort *s_pwBase;

		static public void CreateTableFile()
			{
			MmFile table = table = MmFile.Open(FileMode.CreateNew, Path.Combine(Program.Root, "hint.table"), null, (long) Equation.AllAnswers.Count * Equation.AllEquations.Count * sizeof(ushort));

			using (MmPin pin = table.Pin())
				{
				ushort *pwBase = (ushort *) pin.Pb;

				List<Thread> threads = new List<Thread>();
				int threadCount = Environment.ProcessorCount;
				int equationsDone = 0;
				for(int i=0; i<threadCount; i++)
					{
					threads.Add(ThreadEx.Fork(i, delegate(int a_iThread)
						{
						for(int iAns = a_iThread; iAns < Equation.AllAnswers.Count; iAns += threadCount)
							{
							ushort *pwAns = pwBase + ((long) iAns * Equation.AllEquations.Count);

							for(int iEqu = 0; iEqu < Equation.AllEquations.Count; iEqu++)
								{
								ushort hint = Hint.CalculateHint(Equation.AllAnswers[iAns], Equation.AllEquations[iEqu]);
								*pwAns = hint;
								pwAns++;
								}
							
							int done = Interlocked.Increment(ref equationsDone);
							if (done % 100 == 0)
								MyTrace.WriteLine("Wrote {0} of {1} equations...", done, Equation.AllAnswers.Count);
							}
						}));
					}
				foreach(Thread t in threads)
					t.Join();
				}

			table.Close();
			}

		static public void OpenTable()
			{
			MyTrace.WriteLine("Opening hint table...");
			s_table = MmFile.Open(FileMode.Open, Path.Combine(Program.Root, "hint.table"), null, (long) Equation.AllAnswers.Count * Equation.AllEquations.Count * sizeof(ushort));
			s_pin = s_table.Pin();
			s_pwBase = (ushort *) s_pin.Pb;
			}

		static public void CloseTable()
			{
			if (s_table != null)
				{
				MyTrace.WriteLine("Closing hint table...");
				s_pin.Dispose();
				s_pin = null;
				s_pwBase = null;
				s_table.Close();
				s_table = null;
				}
			}

		static int[] s_usedHints = new int[65536];

		static public List<Equation>[] ListBucketCount(List<Equation> a_answers)
			{
			Array.Clear(s_usedHints);
			List<Equation>[] result = new List<Equation>[a_answers.Count + 1];

			ushort *[] rgpwAns = new ushort *[a_answers.Count];
			for(int iAns = 0; iAns < a_answers.Count; iAns++)
				{
				rgpwAns[iAns] = s_pwBase + ((long) a_answers[iAns].m_answerId * Equation.AllEquations.Count);
				}

			for(int iEqu = 0; iEqu < Equation.AllEquations.Count; iEqu++)
				{
				Equation guess = Equation.AllEquations[iEqu];
				int hintCount = 0;

				for(int iAns = 0; iAns < a_answers.Count; iAns++)
					{
					ushort hint = *(rgpwAns[iAns] + iEqu);

//					DebugEx.Assert(hint == CalculateHint(a_answers[iAns], guess));

					if (hint == 0xFF00)
						{
						hintCount = 0;
						break;
						}
					if (s_usedHints[hint] != ~iEqu)
						{
						hintCount++;
						s_usedHints[hint] = ~iEqu;
						}
					}
				if (hintCount < 2)
					continue;

				if (result[hintCount] == null)
					result[hintCount] = new List<Equation>();
				result[hintCount].Add(guess);
				}

			return result;
			}

		static public Dictionary<ushort, List<Equation>> SplitAnswersByHint(List<Equation> a_answers, Equation a_guess)
			{
			Dictionary<ushort, List<Equation>> dict = new Dictionary<ushort, List<Equation>>();
			
			for(int iAns = 0; iAns < a_answers.Count; iAns++)
				{
				ushort *pwHint = s_pwBase + ((long) a_answers[iAns].m_answerId * Equation.AllEquations.Count) + a_guess.m_equationId;
				ushort hint = *pwHint;

//				DebugEx.Assert(hint == CalculateHint(a_answers[iAns], a_guess));

				List<Equation> bucket;
				if (dict.TryGetValue(hint, out bucket) == false)
					{
					bucket = new List<Equation>();
					dict[hint] = bucket;
					}
				bucket.Add(a_answers[iAns]);
				}
			
			return dict;
			}

		static public ushort CalculateHint(Equation a_answer, Equation a_guess)
			{
			ushort hint = 0;
			if (a_answer.m_twinSetId == a_guess.m_twinSetId)
				return 0xFF00;

			byte[] aCodes = a_answer.m_codes;
			byte[] gCodes = a_guess.m_codes;
			byte[] counts = new byte[16];

			for(int i=0; i<8; i++)
				{
				if (aCodes[i] == gCodes[i])
					hint |= (ushort) (1 << 8+(7-i));
				else
					counts[aCodes[i]]++;
				}

			for(int i=0; i<8; i++)
				{
				if (aCodes[i] != gCodes[i])
					{
					if (counts[gCodes[i]] > 0)
						{
						hint |= (ushort) (1 << 7-i);
						counts[gCodes[i]]--;
						}
					}
				}

			return hint;
			}
		
		static public Equation FindAverageOfTwo(List<Equation> a_answers)
			{
			Array.Clear(s_usedHints);

			ushort *[] rgpwAns = new ushort *[a_answers.Count];
			for(int iAns = 0; iAns < a_answers.Count; iAns++)
				{
				rgpwAns[iAns] = s_pwBase + ((long) a_answers[iAns].m_answerId * Equation.AllEquations.Count);
				}

			for(int iEqu = 0; iEqu < Equation.AllEquations.Count; iEqu++)
				{
				Equation guess = Equation.AllEquations[iEqu];
				int uniqueHints = 0;

				for(int iAns = 0; iAns < a_answers.Count; iAns++)
					{
					ushort hint = *(rgpwAns[iAns] + iEqu);

//					DebugEx.Assert(hint == CalculateHint(a_answers[iAns], guess));

					if (hint == 0xFF00)
						break;
					if (s_usedHints[hint] != ~iEqu)
						{
						s_usedHints[hint] = ~iEqu;
						uniqueHints++;
						}
					}

				if (uniqueHints == a_answers.Count)
					return Equation.AllEquations[iEqu];
				}
			return null;
			}

		static public int Compare(ushort a, ushort b)
			{
			for(int i=0; i<8; i++)
				{
				bool aGreen = (a & 1 << (8+7-i)) != 0;
				bool bGreen = (b & 1 << (8+7-i)) != 0;
				bool aPurple = (a & 1 << (7-i)) != 0;
				bool bPurple = (b & 1 << (7-i)) != 0;

				int aScore = aGreen ? 2 : (aPurple ? 1 : 0);
				int bScore = bGreen ? 2 : (bPurple ? 1 : 0);

				if (aScore < bScore)
					return -1;
				if (aScore > bScore)
					return 1;
				}
			return 0;
			}

		static public string ToColorString(ushort a_hint)
			{
			StringBuilder sb = new StringBuilder();
			for(int i=0; i<8; i++)
				{
				bool green = (a_hint & 1 << (8+7-i)) != 0;
				bool purple = (a_hint & 1 << (7-i)) != 0;
				if (green)
					sb.Append('G');
				else if (purple)
					sb.Append('p');
				else
					sb.Append('.');
				}
			return sb.ToString();
			}

		static public string ToHtml(ushort a_hint, Equation a_guess)
			{
			StringBuilder sb = new StringBuilder();

			sb.Append("<TABLE BORDER=1 CELLPADDING=0 CELLSPACING=0 STYLE='font-family: Consolas, \"Courier New\", Courier, monospace;font-size:10px'><TR>");
			for(int i=0; i<8; i++)
				{
				bool green = (a_hint & 1 << (8+7-i)) != 0;
				bool purple = (a_hint & 1 << (7-i)) != 0;

				char c = Equation.IndexToChar(a_guess.m_codes[i] - 1);

				if (green)
					sb.AppendFormat("<TD STYLE='background-color:green;color:white'>{0}</TD>", c);
				else if (purple)
					sb.AppendFormat("<TD STYLE='background-color:purple;color:white'>{0}</TD>", c);
				else
					sb.AppendFormat("<TD STYLE='background-color:black;color:white'>{0}</TD>", c);
				}
			sb.Append("</TR></TABLE>");

			return sb.ToString();
			}
		}
	}
