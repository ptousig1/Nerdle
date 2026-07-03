using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Patrick;
using Patrick.Csv2;
using Patrick.Json5;

namespace Nerdle
	{
	static internal class Solver6
		{
		static public void Solve()
			{
			File.Delete(Path.Combine(Program.Root, "stats.csv"));
			s_statsFile = CsvFile.Create(Path.Combine(Program.Root, "stats.csv"),
				new CsvHeader("Depth,Count,Ticks,Guess,Average,Winner,IsAnswer"
							+ ",A_Ticks,A_Recursions,A_Theoretical,A_GuessIndex,A_Guess,A_Average"
							+ ",B_Ticks,B_GuessIndex,B_Guess,B_Average"
							+ ",C_Ticks,C_Recursions,C_GuessIndex,C_BestCount,C_LastCount,C_Guess,C_Average"));

			Equation bestGuess;
			Fraction bestAverage = 9999;
			int guessIndex = 0;
			foreach(Equation guess in Equation.AllEquations)
				{
				MyTrace.WriteLine("Guess {0} of {1}...", guessIndex, Equation.AllEquations.Count);
				Fraction guessAverage;
				if (CalculateGuessAverage(1, Equation.AllAnswers, guess, out guessAverage))
					{
					if (guessAverage < bestAverage)
						{
						bestGuess = guess;
						bestAverage = guessAverage;
						}
					}
				}
			}

		static private CsvFile s_statsFile;
		static private int s_statsLine = 0;

		static private bool FindBestGuess(int a_depth, List<Equation> a_answers, out Equation o_bestGuess, out Fraction o_bestAverage)
			{
			o_bestGuess = null;
			o_bestAverage = 9999;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			CsvRow stats = new CsvRow();
			try
				{
				stats["Depth"] = a_depth.ToString();
				stats["Count"] = a_answers.Count.ToString();

				DebugEx.Assert(a_answers.Count > 0);

/*
				if (a_answers.Count <= 6 && a_answers[0].m_twinSetId == a_answers[1].m_twinSetId)
					{
					bool allTwins = true;
					for(int i=2; i<a_answers.Count; i++)
						{
						if (a_answers[0].m_twinSetId != a_answers[i].m_twinSetId)
							{
							allTwins = false;
							break;
							}
						}
					if (allTwins)
						{
						o_bestGuess = a_answers[0];
						o_bestAverage = 1;
						return true;
						}
					}
*/

				if (true)
					{
					Equation bestGuess;
					Fraction bestAverage;
					if (FindBestGuess_A(a_depth, a_answers, out bestGuess, out bestAverage, stats))
						{
						if (bestAverage < o_bestAverage)
							{
							o_bestGuess = bestGuess;
							o_bestAverage = bestAverage;
							stats["Winner"] = "A";
							}
						}
					}

				if (true)
					{
					Equation bestGuess;
					Fraction bestAverage;
					if (FindBestGuess_B(a_depth, a_answers, out bestGuess, out bestAverage, stats))
						{
						if (bestAverage < o_bestAverage)
							{
							o_bestGuess = bestGuess;
							o_bestAverage = bestAverage;
							stats["Winner"] = "B";
							}
						}
					}

				if (o_bestAverage > 2)
					{
					Equation bestGuess;
					Fraction bestAverage;
					if (FindBestGuess_C(a_depth, a_answers, out bestGuess, out bestAverage, stats))
						{
						if (bestAverage < o_bestAverage)
							{
							o_bestGuess = bestGuess;
							o_bestAverage = bestAverage;
							stats["Winner"] = "C";
							}
						}
					}

				DebugEx.Assert(o_bestGuess != null);
				DebugEx.Assert(o_bestAverage < 10);
				return true;
				}
			finally
				{
				sw.Stop();
				stats["Ticks"] = sw.ElapsedTicks.ToString();
				if (o_bestGuess != null)
					{
					stats["Guess"] = o_bestGuess.ToString();
					stats["IsAnswer"] = a_answers.Contains(o_bestGuess).ToString();
					stats["Average"] = o_bestAverage.ToDecimalString();
					}
				s_statsFile.AppendRow(stats);

				s_statsLine++;
				if (s_statsLine % 1000 == 0)
					MyTrace.WriteLine("{0} stats lines written...", s_statsLine);
				}
			}

		static private bool FindBestGuess_A(int a_depth, List<Equation> a_answers, out Equation o_bestGuess, out Fraction o_bestAverage, CsvRow a_stats)
			{
			o_bestGuess = null;
			o_bestAverage = 9999;

			int recursions = 0;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			try
				{
				Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
				a_stats["A_Theoretical"] = theoreticalBest.ToDecimalString();

				int guessIndex = 0;
				foreach(Equation answer in a_answers)
					{
					foreach(Equation guess in answer.m_twins)
						{
						guessIndex++;
						Fraction guessAverage;
						recursions++;
						if (CalculateGuessAverage(a_depth, a_answers, guess, out guessAverage))
							{
							if (guessAverage == theoreticalBest)
								{
								o_bestGuess = guess;
								o_bestAverage = guessAverage;
								a_stats["A_GuessIndex"] = guessIndex.ToString();
								return true;
								}
							else if (guessAverage < o_bestAverage)
								{
								o_bestGuess = guess;
								o_bestAverage = guessAverage;
								}
							}
						}
					}

				a_stats["A_GuessIndex"] = guessIndex.ToString();

				if (o_bestAverage <= 2)
					return true;
				}
			finally
				{
				sw.Stop();
				a_stats["A_Ticks"] = sw.ElapsedTicks.ToString();
				a_stats["A_Recursions"] = recursions.ToString();
				if (o_bestGuess != null)
					{
					a_stats["A_Guess"] = o_bestGuess.ToString();
					a_stats["A_Average"] = o_bestAverage.ToDecimalString();
					}
				}
			return true;
			}

		static private bool FindBestGuess_B(int a_depth, List<Equation> a_answers, out Equation o_bestGuess, out Fraction o_bestAverage, CsvRow a_stats)
			{
			o_bestGuess = null;
			o_bestAverage = 9999;

//			if (a_answers.Count > 8)
//				return false;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			try
				{
				// This assumes that all guesses within a_answers (and their twins) have been tried
				
				// 7+11-9=9
				// 9+1-10=0
				// 9+9-17=1

				// 7..1-9.9
				// .....0.0
				// ..9..7.1

				List<List<Equation>> lists = new List<List<Equation>>();

				for(int i=0; i<8; i++)
					{
					int[] indexes = new int[16];
					int count = 0;
					for(int j=0; j<a_answers.Count; j++)
						{
						byte code = a_answers[j].m_codes[i];
						if (indexes[code] == 0)
							{
							indexes[code] = j + 1;
							count++;
							}
						else if (indexes[code] > 0)
							{
							indexes[code] = -1;
							count--;
							}
						}
					if (count == 0)
						continue;

					for(byte j=1; j<16; j++)
						{
						if (indexes[j] > 0)
							lists.Add(Equation.RestrictedLists[(i*16)+j]);
						}
					}

				lists.Sort(delegate(List<Equation> a, List<Equation> b) { return Int32Ex.Compare(a.Count, b.Count); });

				int guessIndex = 0;
				foreach(List<Equation> list in lists)
					{
					foreach(Equation guess in list)
						{
						guessIndex++;
						Fraction guessAverage = 1;

						Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
						if (buckets.Count == 1)
							{
							if (buckets.ContainsKey(0xFF00))
								DebugEx.SelfHalt();
							else
								continue;
							}
						if (buckets.Count == a_answers.Count)
							{
							o_bestGuess = guess;
							o_bestAverage = 2;
							a_stats["B_GuessIndex"] = guessIndex.ToString();
							return true;
							}

						foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
							{
							Fraction bucketFraction = new Fraction(pair.Value.Count, a_answers.Count);
							Fraction bucketAverage = 0;
							if (pair.Key == 0xFF00)
								bucketAverage = 0;
							else if (pair.Value.Count == 1)
								bucketAverage = 1;
							else if (pair.Value.Count == 2)
								{
								if (pair.Value[0].m_twinSetId == pair.Value[1].m_twinSetId)
									bucketAverage = 1;
								else
									bucketAverage = new Fraction(3, 2);
								}
							else
								{
								guessAverage = 9999;
								break;
								}

							guessAverage += bucketAverage * bucketFraction;
							if (guessAverage > o_bestAverage)
								break;
							}

						if (guessAverage < o_bestAverage)
							{
							o_bestGuess = guess;
							o_bestAverage = guessAverage;

							if (o_bestAverage <= 2)
								{
								a_stats["B_GuessIndex"] = guessIndex.ToString();
								return true;
								}
							}
						}
					}
				
				a_stats["B_GuessIndex"] = guessIndex.ToString();
				}
			finally
				{
				sw.Stop();
				a_stats["B_Ticks"] = sw.ElapsedTicks.ToString();
				if (o_bestGuess != null)
					{
					a_stats["B_Guess"] = o_bestGuess.ToString();
					a_stats["B_Average"] = o_bestAverage.ToDecimalString();
					}
				}
			return true;
			}

		static private bool FindBestGuess_C(int a_depth, List<Equation> a_answers, out Equation o_bestGuess, out Fraction o_bestAverage, CsvRow a_stats)
			{
			o_bestGuess = null;
			o_bestAverage = 9999;

			int recursions = 0;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			try
				{
				List<Equation>[] counts = Hint.ListBucketCount(a_answers);
				int guessIndex = 0;
				int maxCounts = 5;
				for(int i=counts.Length-1; i>=0; i--)
					{
					if (counts[i] == null)
						continue;

					maxCounts--;
					if (maxCounts == 0)
						{
						a_stats["C_LastCount"] = i.ToString();
						a_stats["C_GuessIndex"] = guessIndex.ToString();
						return true;
						}

					foreach(Equation guess in counts[i])
						{
						guessIndex++;

						Fraction guessAverage;
						recursions++;
						if (CalculateGuessAverage(a_depth, a_answers, guess, out guessAverage))
							{
							if (guessAverage < o_bestAverage)
								{
								o_bestGuess = guess;
								o_bestAverage = guessAverage;
								a_stats["C_BestCount"] = i.ToString();
								maxCounts = 5;

								if (o_bestAverage <= 2)
									{
									a_stats["C_GuessIndex"] = guessIndex.ToString();
									return true;
									}
								}
							}
						}
					}
				}
			finally
				{
				sw.Stop();
				a_stats["C_Ticks"] = sw.ElapsedTicks.ToString();
				a_stats["C_Recursions"] = recursions.ToString();
				if (o_bestGuess != null)
					{
					a_stats["C_Guess"] = o_bestGuess.ToString();
					a_stats["C_Average"] = o_bestAverage.ToDecimalString();
					}
				}
			return true;
			}

		static private bool CalculateGuessAverage(int a_depth, List<Equation> a_answers, Equation a_guess, out Fraction o_guessAverage)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1)
				{
				if (buckets.ContainsKey(0xFF00))
					{
					o_guessAverage = 1;
					return true;
					}
				else
					{
					o_guessAverage = 9999;
					return false;	// This guess would make no progress
					}
				}

			o_guessAverage = 1;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
				{
				bucketIndex++;

				Fraction bucketFraction = new Fraction(pair.Value.Count, a_answers.Count);
				Fraction bucketAverage = 0;
				if (pair.Key == 0xFF00)
					bucketAverage = 0;
				else if (pair.Value.Count == 1)
					bucketAverage = 1;
				else if (pair.Value.Count == 2)
					{
					if (pair.Value[0].m_twinSetId == pair.Value[1].m_twinSetId)
						bucketAverage = 1;
					else
						bucketAverage = new Fraction(3, 2);
					}
				else
					{
					Equation bucketGuess;
					if (FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage) == false)
						return false;
					}

				o_guessAverage += bucketAverage * bucketFraction;
				}

			return true;
			}

		static private void CalculateBestAverageOfSmallSet(List<Equation> a_answers, out Equation o_bestGuess, out Fraction o_bestAverage)
			{
			// This assumes that all guesses within a_answers (and their twins) have been tried

			DebugEx.Nop();

			// 7+11-9=9
			// 9+1-10=0
			// 9+9-17=1

			// 7..1-9.9
			// .....0.0
			// ..9..7.1

			// ..9..... = 20932
			// 7....... = 24821
			// .....0.. = 78177

			List<List<Equation>> lists = new List<List<Equation>>();

			for(int i=0; i<8; i++)
				{
				int[] indexes = new int[16];
				int count = 0;
				for(int j=0; j<a_answers.Count; j++)
					{
					byte code = a_answers[j].m_codes[i];
					if (indexes[code] == 0)
						{
						indexes[code] = j + 1;
						count++;
						}
					else if (indexes[code] > 0)
						{
						indexes[code] = -1;
						count--;
						}
					}
				if (count == 0)
					continue;

				for(byte j=1; j<16; j++)
					{
					if (indexes[j] > 0)
						lists.Add(Equation.RestrictedLists[(i*16)+j]);
					}
				}

			lists.Sort(delegate(List<Equation> a, List<Equation> b) { return Int32Ex.Compare(a.Count, b.Count); });

			o_bestGuess = null;
			o_bestAverage = 9999;
			int guessIndex = 0;
			foreach(List<Equation> list in lists)
				{
				foreach(Equation guess in list)
					{
					guessIndex++;
					Fraction guessAverage = 1;

					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
					if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
						continue;

					foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
						{
						Fraction bucketFraction = new Fraction(pair.Value.Count, a_answers.Count);
						Fraction bucketAverage = 0;
						if (pair.Key == 0xFF00)
							bucketAverage = 0;
						else if (pair.Value.Count == 1)
							bucketAverage = 1;
						else if (pair.Value.Count == 2)
							{
							if (pair.Value[0].m_twinSetId == pair.Value[1].m_twinSetId)
								bucketAverage = 1;
							else
								bucketAverage = new Fraction(3, 2);
							}
						else
							{
							guessAverage = 9999;
							break;
							}

						guessAverage += bucketAverage * bucketFraction;
						if (guessAverage > o_bestAverage)
							break;
						}

					if (guessAverage < o_bestAverage)
						{
						o_bestGuess = guess;
						o_bestAverage = guessAverage;

						if (o_bestAverage <= 2)
							return;
						}
					}
				}
			}
		}
	}
