using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Patrick;
using Patrick.Csv2;
using Patrick.Json5;

namespace Nerdle
	{
	internal static class Solver2
		{
		static private Bag<int> s_bucketSizes = new Bag<int>();
		static public void Solve()
			{
			List<Equation> guesses = ChooseFirstGuess();
			foreach(Equation guess in guesses)
				Console.WriteLine("First guess = {0}", guess);
			}

		static private List<Equation> ChooseFirstGuess()
			{
			List<Equation> bestGuesses = new List<Equation>();
			Fraction bestAverage = 9999;
			foreach(Equation guess in Equation.AllEquations)
				{
				Fraction guessAverage = 1;
				int bucketIndex = 0;

				MyTrace.WriteLine("Trying first guess {0}...", guess);
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(Equation.AllAnswers, guess);
				if (buckets.Count > 1 || buckets.ContainsKey(0xFF00))
					{
					foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
						{
						MyTrace.WriteLine("Working on bucket {0} of {1}...", bucketIndex++, buckets.Count);

						Fraction bucketAverage = 0;
						if (pair.Key == 0xFF00)
							bucketAverage = 0;
						else if (pair.Value.Count == 1)
							bucketAverage = 1;
						else
							bucketAverage = CalculateSecondGuess(pair.Value);

						guessAverage += bucketAverage * new Fraction(pair.Value.Count, Equation.AllEquations.Count);
						if (guessAverage > bestAverage)
							break;
						}
					}
				
				MyTrace.WriteLine("First guess {0} gave an average of {1}...", guess, (decimal) guessAverage);

				if (guessAverage < bestAverage)
					bestGuesses.Clear();
				if (guessAverage <= bestAverage)
					bestGuesses.Add(guess);
				}

			return bestGuesses;
			}

		static private Bag<int> s_secondDistrib = new Bag<int>();
		static private Fraction CalculateSecondGuess(List<Equation> a_answers)
			{
			s_secondDistrib.Add(a_answers.Count);

			Dictionary<Equation, int> counts = new Dictionary<Equation, int>();
			foreach(Equation guess in Equation.AllEquations)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;
				counts[guess] = buckets.Count;
				}

			List<Equation> guessesToTry = counts.Keys.ToList();
			guessesToTry.Sort(delegate(Equation a, Equation b)
				{
				return Int32Ex.Compare(counts[b], counts[a]);
				});

			Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
			Fraction bestAverage = 9999;
			int bestBucketCount = 0;
			int guessIndex = 0;
			foreach(Equation guess in guessesToTry)
				{
				guessIndex++;
				Fraction guessAverage = 9999;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count < bestBucketCount - 10)
					break;

				if (buckets.Count > 1 || buckets.ContainsKey(0xFF00))
					{
					List<KeyValuePair<ushort, List<Equation>>> sortedBuckets = buckets.ToList();
					sortedBuckets.Sort(delegate (KeyValuePair<ushort, List<Equation>> a, KeyValuePair<ushort, List<Equation>> b)
						{
						return Int32Ex.Compare(b.Value.Count, a.Value.Count);
						});

					guessAverage = 1;
					int bucketIndex = 0;
					foreach(KeyValuePair<ushort, List<Equation>> pair in sortedBuckets)
						{
						bucketIndex++;

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
							bucketAverage = CalculateThirdGuess(pair.Value);

						guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
//						if (guessAverage > bestAverage)
//							break;
						}
					}

				if (guessAverage <= bestAverage)
					{
					bestAverage = guessAverage;
					bestBucketCount = buckets.Count;
					MyTrace.WriteLine("Guess {0} of {1} with {2} buckets gave an average of {3}", guessIndex, guessesToTry.Count, buckets.Count, (decimal) guessAverage);

					if (bestAverage <= 2)
						{
						if (buckets.ContainsKey(0xFF00))
							{
							DebugEx.Assert(bestAverage >= theoreticalBest);
							if (bestAverage == theoreticalBest)
								{
								MyTrace.WriteLine("This is the theoretical best");
								break;
								}
							}
						else
							{
							DebugEx.Assert(bestAverage >= 2);
							if (bestAverage == 2)
								{
								MyTrace.WriteLine("This is the theoretical best without the guess being an answer");
								break;
								}
							}
						}
					}
				else if (guessIndex % 1000 == 0)
					{
					MyTrace.WriteLine("Guess {0} of {1} with {2} buckets gave an average of {3}", guessIndex, guessesToTry.Count, buckets.Count, (decimal) guessAverage);
					}
				}
			DebugEx.Assert(bestAverage < 9999);
			return bestAverage;
			}

		static private Bag<int> s_thirdDistrib = new Bag<int>();
		static private Fraction CalculateThirdGuess(List<Equation> a_answers)
			{
			s_thirdDistrib.Add(a_answers.Count);

			Dictionary<Equation, int> counts = new Dictionary<Equation, int>();
			int guessIndex = 0;
			foreach(Equation guess in Equation.AllEquations)
				{
				guessIndex++;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;

				Fraction guessAverage = 1;
				bool bestPossibleAverage = true;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					{
					Fraction bucketAverage;
					if (pair.Key == 0xFF00)
						bucketAverage = 0;
					else if (pair.Value.Count == 1)
						bucketAverage = 1;
					else if (pair.Value.Count == 2)
						{
						if (pair.Value[0].m_twinSetId == pair.Value[1].m_twinSetId)
							bucketAverage = 1;
						else
							{
							bucketAverage = new Fraction(3, 2);
							bestPossibleAverage = false;
							break;
							}
						}
					else
						{
						bucketAverage = 9999;
						bestPossibleAverage = false;
						break;
						}

					guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
					}
				if (bestPossibleAverage)
					return guessAverage;

				counts[guess] = buckets.Count;
				}

			List<Equation> guessesToTry = counts.Keys.ToList();
			guessesToTry.Sort(delegate(Equation a, Equation b)
				{
				return Int32Ex.Compare(counts[b], counts[a]);
				});

			Fraction bestAverage = 9999;
			guessIndex = 0;
			foreach(Equation guess in guessesToTry)
				{
				guessIndex++;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00))
					DebugEx.SelfHalt();

				List<KeyValuePair<ushort, List<Equation>>> sortedBuckets = buckets.ToList();
				sortedBuckets.Sort(delegate (KeyValuePair<ushort, List<Equation>> a, KeyValuePair<ushort, List<Equation>> b)
					{
					return Int32Ex.Compare(b.Value.Count, a.Value.Count);
					});

				Fraction guessAverage = 1;
				foreach(KeyValuePair<ushort, List<Equation>> pair in sortedBuckets)
					{
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
						bucketAverage = CalculateFourthGuess(pair.Value);

					guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
					if (guessAverage > bestAverage)
						break;
					}

				if (guessAverage < bestAverage)
					bestAverage = guessAverage;
				if (bestAverage == 2)
					break;
				}
			DebugEx.Assert(bestAverage < 9999);
			return bestAverage;
			}

		static private Fraction CalculateFourthGuess(List<Equation> a_answers)
			{
//			DebugEx.SelfHalt();
			return 9999;
			}

		static private Fraction CalculateBestAverage(List<Equation> a_answers)
			{
			s_bucketSizes.Add(a_answers.Count);

			if (a_answers.Count == 1)
				return 1;

			if (a_answers.Count == 2)
				{
				if (a_answers[0].m_twinSetId == a_answers[1].m_twinSetId)
					return 1;
				else
					return new Fraction(3, 2);
				}

			Fraction bestAverage = TestTheoreticalBestUsingAnswers(a_answers);
			if (bestAverage < 9999)
				return bestAverage;

			return CalculateBestAverageTheHardWay(a_answers);
			}

		static private Fraction TestTheoreticalBestUsingAnswers(List<Equation> a_answers)
			{
			Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);

			foreach(Equation guess in a_answers)
				{
				Fraction guessAverage;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1)
					{
					if (buckets.ContainsKey(0xFF00))
						guessAverage = 1;
					else
						guessAverage = 9999;
					}
				else
					{
					List<KeyValuePair<ushort, List<Equation>>> sortedBuckets = buckets.ToList();
					sortedBuckets.Sort(delegate (KeyValuePair<ushort, List<Equation>> a, KeyValuePair<ushort, List<Equation>> b)
						{
						return Int32Ex.Compare(b.Value.Count, a.Value.Count);
						});

					guessAverage = 1;
					foreach(KeyValuePair<ushort, List<Equation>> pair in sortedBuckets)
						{
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
							bucketAverage = CalculateBestAverage(pair.Value);

						guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
						if (guessAverage > theoreticalBest)
							break;
						}
					}

				DebugEx.Assert(guessAverage >= theoreticalBest);
				if (guessAverage == theoreticalBest)
					return guessAverage;
				}
			return 9999;
			}

		static private Fraction CalculateBestAverageTheHardWay(List<Equation> a_answers)
			{
			Dictionary<Equation, int> counts = new Dictionary<Equation, int>();
			foreach(Equation guess in Equation.AllEquations)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;
				counts[guess] = buckets.Count;
				}

			List<Equation> guessesToTry = counts.Keys.ToList();
			guessesToTry.Sort(delegate(Equation a, Equation b)
				{
				return Int32Ex.Compare(counts[b], counts[a]);
				});

			Fraction bestAverage = 9999;
			int guessIndex = 0;
			foreach(Equation guess in guessesToTry)
				{
				guessIndex++;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;

				List<KeyValuePair<ushort, List<Equation>>> sortedBuckets = buckets.ToList();
				sortedBuckets.Sort(delegate (KeyValuePair<ushort, List<Equation>> a, KeyValuePair<ushort, List<Equation>> b)
					{
					return Int32Ex.Compare(b.Value.Count, a.Value.Count);
					});

				Fraction guessAverage = 1;
				foreach(KeyValuePair<ushort, List<Equation>> pair in sortedBuckets)
					{
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
						bucketAverage = CalculateBestAverage(pair.Value);

					guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
					if (guessAverage > bestAverage)
						break;
					}

				if (guessAverage < bestAverage)
					bestAverage = guessAverage;
				if (bestAverage == 2)
					break;
				}
			DebugEx.Assert(bestAverage < 9999);
			return bestAverage;
			}
		}
	}
