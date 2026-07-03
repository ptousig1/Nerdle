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
	internal static class Solver4
		{
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
						MyTrace.WriteLine("Working on bucket {0} of {1} of size {2}...", bucketIndex++, buckets.Count, pair.Value.Count);

						Fraction bucketAverage = 0;
						if (pair.Key == 0xFF00)
							bucketAverage = 0;
						else if (pair.Value.Count == 1)
							bucketAverage = 1;
						else
							bucketAverage = ChooseSecondGuess(pair.Value);

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

		static private Fraction ChooseSecondGuess(List<Equation> a_answers)
			{
			if (a_answers.Count == 57)
				LogAnswerList(a_answers);

			Equation bestGuess = null;
			Fraction bestAverage = 9999;

			Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
			foreach(Equation guess in a_answers)
				{
				Fraction guessAverage = CalculateSecondGuessAverage(a_answers, guess);
				if (guessAverage == theoreticalBest)
					return guessAverage;
				if (guessAverage < bestAverage)
					{
					bestGuess = guess;
					bestAverage = guessAverage;
//					MyTrace.WriteLine("Guess {0} gave an average of {1}", guess, (decimal) guessAverage);
					}
				}

			HashSet<Equation> tried = new HashSet<Equation>(a_answers);

			ushort[] wanted = new ushort[8];
			ushort overallWanted = 0;
			foreach(Equation guess in a_answers)
				{
				for(int i=0; i<8; i++)
					wanted[i] |= guess.m_masks[i];
				}
			for(int i=0; i<8; i++)
				{
				if (wanted[i].BitSetCount() == 1)
					wanted[i] = 0;
				}
			for(int i=0; i<8; i++)
				{
				overallWanted |= wanted[i];
				}

			Dictionary<Equation, int> scores = new Dictionary<Equation, int>();
			foreach(Equation guess in Equation.AllEquations)
				{
				if (tried.Contains(guess))
					continue;

				int score = 0;
				for(int i=0; i<8; i++)
					score += ((guess.m_masks[i] & wanted[i]) != 0) ? 5 : 0;
				score += ((ushort) (guess.m_overallMask & overallWanted)).BitSetCount();
				if (score > 30)
					scores.Add(guess, score);
				}

			List<Equation> guessesToTry = new List<Equation>(scores.Keys);
			guessesToTry.Sort(delegate(Equation a, Equation b) { return Int32Ex.Compare(scores[b], scores[a]); });
			guessesToTry = guessesToTry.SafeSlice(0, 2000);

			MyTrace.WriteLine("Guess {0} gave an average of {1}", bestGuess, (decimal) bestAverage);
			int guessIndex = 0;
			int lastBestIndex = 0;
			foreach(Equation guess in guessesToTry)
				{
				DebugEx.Assert(guess != null);
				guessIndex++;

//				if (guessIndex > lastBestIndex + 10000)
//					break;

				Fraction guessAverage = CalculateSecondGuessAverage(a_answers, guess);
				if (guessAverage < bestAverage)
					{
					bestAverage = guessAverage;
					lastBestIndex = guessIndex;
					MyTrace.WriteLine("Guess {0} of {1} gave an average of {2}", guessIndex, guessesToTry.Count, (decimal) guessAverage);
					}

//				if (guessIndex % 1000 == 0)
//					MyTrace.WriteLine("Guess {0} of {1} gave an average of {2}", guessIndex, guessesToTry.Count, (decimal) guessAverage);
				}

			DebugEx.Assert(bestAverage < 9);
			return bestAverage;
			}

		static private Fraction CalculateSecondGuessAverage(List<Equation> a_answers, Equation a_guess)
			{
			Fraction guessAverage = 9999;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
				return 9999;	// This guess would make no progress

			guessAverage = 1;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
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
					bucketAverage = ChooseThirdGuess(pair.Value);

				guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
				}

			return guessAverage;
			}

		static private bool FastCalculateSecondGuessAverage(List<Equation> a_answers, Equation a_guess, out Fraction o_guessAverage, out int o_bucketCount)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
				{
				o_bucketCount = 9999;
				o_guessAverage = 9999;
				return true;	// This guess would make no progress
				}

			o_guessAverage = 1;
			o_bucketCount = buckets.Count;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
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
					return false;

				o_guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
				}

			return true;
			}

		static private Fraction ChooseThirdGuess(List<Equation> a_answers)
			{
			Equation bestGuess = null;
			Fraction bestAverage = 9999;
			Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
			foreach(Equation guess in a_answers)
				{
				Fraction guessAverage = CalculateThirdGuessAverage(a_answers, guess);
				if (guessAverage == theoreticalBest)
					return guessAverage;
				if (guessAverage < bestAverage)
					{
					bestGuess = guess;
					bestAverage = guessAverage;
//					MyTrace.WriteLine("Guess {0} gave an average of {1}", guess, (decimal) guessAverage);
					}
				}

			HashSet<Equation> tried = new HashSet<Equation>(a_answers);

			int guessIndex = 0;
			foreach(Equation guess in Equation.AllEquations)
				{
				DebugEx.Assert(guess != null);
				guessIndex++;

				if (tried.Contains(guess))
					continue;

				Fraction guessAverage;
				int bucketCount;
				if (FastCalculateThirdGuessAverage(a_answers, guess, out guessAverage, out bucketCount))
					{
					if (guessAverage == 2)
						return guessAverage;
					if (guessAverage < bestAverage)
						{
						bestGuess = guess;
						bestAverage = guessAverage;
						}
					}
				}

			return bestAverage;
			}

		static private Fraction CalculateThirdGuessAverage(List<Equation> a_answers, Equation a_guess)
			{
			Fraction guessAverage = 9999;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
				return 9999;	// This guess would make no progress

			guessAverage = 1;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
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
					return 9999;

				guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
				}

			return guessAverage;
			}

		static private bool FastCalculateThirdGuessAverage(List<Equation> a_answers, Equation a_guess, out Fraction o_guessAverage, out int o_bucketCount)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
				{
				o_bucketCount = 9999;
				o_guessAverage = 9999;
				return true;	// This guess would make no progress
				}

			o_guessAverage = 1;
			o_bucketCount = buckets.Count;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
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
					return false;

				o_guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
				}

			return true;
			}

		static private void LogAnswerList(List<Equation> a_answers)
			{
			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "stats", "answers.txt"));
			sw.WriteLine("Answer List");
			sw.WriteLine("-----------");
			foreach(Equation equ in a_answers)
				sw.WriteLine(equ.ToString());

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

			sw.WriteLine();
			sw.WriteLine("Guess    Buckets Average Best");
			sw.WriteLine("-------- ------- ------- ----");
			Fraction bestAverage = 9999;
			int guessIndex = 0;
			foreach(Equation guess in guessesToTry)
				{
				guessIndex++;

				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);		

				Fraction guessAverage = 9999;
				if (buckets.Count > 1 || buckets.ContainsKey(0xFF00))
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
							bucketAverage = ChooseThirdGuess(pair.Value);

						guessAverage += bucketAverage * new Fraction(pair.Value.Count, a_answers.Count);
						}

					if (guessAverage < bestAverage)
						bestAverage = guessAverage;

					sw.WriteLine("{0,8} {1,7} {2,7} {3,4}", guess, buckets.Count, (decimal) guessAverage, guessAverage == bestAverage ? "best" : "");
					sw.Flush();

					if (a_answers.Contains(guess))
						{
						if (bestAverage == Equation.CalculateTheoreticalBestAverage(a_answers))
							break;
						}
					else
						{
						if (bestAverage == 2)
							break;
						}

					}
				}
			
			sw.Close();
			}
		}
	}
