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
	internal unsafe class Solver7
		{
		enum Result
			{
			Failure = 1,
			Optimal = 2,
			Best = 3,
			Reasonable = 4,
			}

		static private Lock s_lock = new Lock();
		static private CsvFile s_statsFile;
		static private int s_statsLine = 0;

		private int[] m_usedHints = new int[65536];

		static public void Solve()
			{
			MyTrace.WriteLine("Solver7 working...");

			using(s_lock.WriteLock())
				{
				File.Delete(Path.Combine(Program.Root, "stats.csv"));	
				s_statsFile = CsvFile.Create(Path.Combine(Program.Root, "stats.csv"),
					new CsvHeader("Depth,Count,Ticks,Result,Guess,Average,IsAnswer,Pass,Equations"));
				}

			Solver7 me = new Solver7();
			me.WorkerThread();
			}

		public Solver7()
			{
			}

		private void WorkerThread()
			{
			Equation guess;
			Fraction average;
			Result res = FindBestGuess(0, Equation.AllAnswers, out guess, out average);
			MyTrace.WriteLine("Result = {0}", res);
			MyTrace.WriteLine("Guess = {0}", guess);
			MyTrace.WriteLine("Average = {0}", average);
			}

		private Result FindBestGuess(int a_depth, List<Equation> a_answers, out Equation o_guess, out Fraction o_average)
			{
			Result result = Result.Failure;
			o_guess = null;
			o_average = 9999;
			int bucketCount = 9999;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			CsvRow stats = new CsvRow();
			try
				{
				stats["Depth"] = a_depth.ToString();
				stats["Count"] = a_answers.Count.ToString();

				if (a_answers.Count < Equation.AllAnswers.Count)
					{
					//
					// Try all the equations that are part of a_answers (or their twins)
					//
					Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
					foreach(Equation answer in a_answers)
						{
						foreach(Equation guess in answer.m_twins)
							{
							Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
							if (buckets.Count == 1)
								{
								if (buckets.ContainsKey(0xFF00))
									{
									o_guess = guess;
									o_average = 1;
									stats["Pass"] = "Answers";
									result = Result.Optimal;
									return result;
									}
								continue;
								}

							Fraction guessAverage = 1;
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
									Equation bucketGuess;
									Result bucketResult = FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
									if (bucketResult == Result.Failure)
										{
										DebugEx.SelfHalt();
										guessAverage = 9999;
										break;
										}
									}

								guessAverage += bucketAverage * bucketFraction;
								}

							if (guessAverage < o_average)
								{
								o_guess = guess;
								o_average = guessAverage;
								bucketCount = buckets.Count;

								DebugEx.Assert(guessAverage >= theoreticalBest);
								if (guessAverage == theoreticalBest)
									{
									stats["Pass"] = "Answers";
									result = Result.Optimal;
									return result;
									}
								}
							}
						}

					if (o_average <= 2)
						{
						stats["Pass"] = "Answers";
						result = Result.Best;
						return result;
						}
						
					//
					// Do a fast pass through all the equations looking for the optimal average of 2.
					//
					Equation twoGuess = Hint.FindAverageOfTwo(a_answers);
					if (twoGuess != null)
						{
						o_guess = twoGuess;
						o_average = 2;
						stats["Pass"] = "Fast";
						result = Result.Optimal;
						return result;
						}

					//
					// Try every guess, starting with the ones with most number of buckets.
					//
					DebugEx.Assert(a_answers.Count < 256);
					List<Equation>[] bucketCounts = Hint.ListBucketCount(a_answers);
					int tried = 0;
					for(int iSize=bucketCounts.Length-1; iSize >= 1; iSize--)
						{
						if (iSize < bucketCount - 4 && tried > 1000)
							break;

						List<Equation> equations = bucketCounts[iSize];
						if (equations == null)
							continue;

						for(int iEqu=0; iEqu < equations.Count; iEqu++)
							{
							Equation guess = equations[iEqu];
							tried++;

							Dictionary<ushort,List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
							DebugEx.Assert(buckets.Count == iSize);

							Fraction guessAverage = 1;
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
									Equation bucketGuess;
									Result bucketResult = FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
									if (bucketResult == Result.Failure)
										{
										DebugEx.SelfHalt();
										guessAverage = 9999;
										break;
										}
									}

								guessAverage += bucketAverage * bucketFraction;
								}

							DebugEx.Assert(guessAverage >= 2);

							if (guessAverage < o_average)
								{
								o_guess = guess;
								o_average = guessAverage;
								bucketCount = buckets.Count;
								tried = 0;
								}
							}
						}

					stats["Pass"] = "Slow";
					stats["Equations"] = Equation.AllEquations.Count.ToString();
					result = Result.Best;
					return result;
					}
				else
					{
					//
					// This is the first guess, we simply try all possible equations
					//
					DebugEx.Assert(a_depth == 0);
					for(int iEqu=0; iEqu<Equation.AllEquations.Count; iEqu++)
						{
						Equation guess = Equation.AllEquations[iEqu];
						Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);

						if (buckets.Count == 1)
							{
							DebugEx.SelfHalt();
							if (buckets.ContainsKey(0xFF00))
								{
								o_guess = guess;
								o_average = 1;
								stats["Pass"] = "Full";
								result = Result.Optimal;
								}
							else
								{
								result = Result.Failure;
								}
							return result;
							}

						Fraction guessAverage = 1;
						foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
							{
							MyTrace.WriteLine("Processing top bucket with {0} answers...", pair.Value.Count);

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
								Result bucketResult = FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
								if (bucketResult == Result.Failure)
									{
									DebugEx.SelfHalt();
									guessAverage = 9999;
									break;
									}
								}

							guessAverage += bucketAverage * bucketFraction;
							}

						DebugEx.Assert(guessAverage >= 2);

						if (guessAverage < o_average)
							{
							o_guess = guess;
							o_average = guessAverage;
							bucketCount = buckets.Count;
							}
						}
					
					stats["Pass"] = "Full";
					stats["Equations"] = Equation.AllEquations.Count.ToString();
					result = Result.Best;
					return result;
					}
				}
			finally
				{
				sw.Stop();
				stats["Ticks"] = sw.ElapsedTicks.ToString();
				stats["Result"] = result.ToString();
				if (o_guess != null)
					{
					stats["Guess"] = o_guess.ToString();
					stats["IsAnswer"] = a_answers.Contains(o_guess).ToString();
					stats["Average"] = o_average.ToDecimalString();
					}
				using(s_lock.WriteLock())
					{
					s_statsFile.AppendRow(stats);
					s_statsLine++;
					if (s_statsLine % 1000 == 0)
						MyTrace.WriteLine("{0} stats lines written...", s_statsLine);
					}
				}
			}
		}
	}
		