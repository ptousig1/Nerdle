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
	static internal class Solver5
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

			using (Progress progress = new Progress("Guess", Equation.AllEquations.Count))
				{
				progress.Increment();
				foreach(Equation guess in Equation.AllEquations)
					{
					Fraction guessAverage = 1;
					int bucketIndex = 0;

					MyTrace.WriteLine("First: Trying guess {0}...", guess);
					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(Equation.AllAnswers, guess);
					if (buckets.Count > 1 || buckets.ContainsKey(0xFF00))
						{
						using (Progress progress2 = new Progress("Bucket", buckets.Count))
							{
							foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
								{
								progress2.Increment();
								MyTrace.WriteLine("First: Working on bucket {0} of {1} of size {2}...", bucketIndex++, buckets.Count, pair.Value.Count);

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
						}
				
					MyTrace.WriteLine("First: Guess {0} gave an average of {1}...", guess, (decimal) guessAverage);

					if (guessAverage < bestAverage)
						bestGuesses.Clear();
					if (guessAverage <= bestAverage)
						bestGuesses.Add(guess);
					}
				}

			return bestGuesses;
			}

		static private Fraction ChooseSecondGuess(List<Equation> a_answers)
			{
			List<Equation>[] counts = Hint.ListBucketCount(a_answers);

			Fraction bestAverage = 9999;

			using (Progress progress = new Progress("BucketSize", counts.Length))
				{
				for(int i=counts.Length-1; i>=0; i--)
					{
					progress.Increment();

					if (counts[i] == null)
						continue;

					using (Progress progress2 = new Progress("BucketOfSize", counts[i].Count))
						{
						foreach(Equation guess in counts[i])
							{
							progress2.Increment();

//							MyTrace.WriteLine("Second: Working on guess {0}...", guessIndex++);

							Fraction guessAverage = CalculateSecondGuessAverage(a_answers, guess);
							if (guessAverage < bestAverage)
								{
								bestAverage = guessAverage;
								MyTrace.WriteLine("Second: Guess {0} gave an average of {1}", guess, (decimal) guessAverage);
								}
							}
						}
					}
				}

			return bestAverage;
			}

		static private Fraction CalculateSecondGuessAverage(List<Equation> a_answers, Equation a_guess)
			{
			Fraction guessAverage = 9999;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, a_guess);
			if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
				return 9999;	// This guess would make no progress

			using (Progress progress = new Progress("Bucket", buckets.Count))
				{
				guessAverage = 1;
				int bucketIndex = 0;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					{
					progress.Increment();
					bucketIndex++;

	//				MyTrace.WriteLine("Second: Working on bucket {0} of {1} of size {2}...", bucketIndex++, buckets.Count, pair.Value.Count);

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
				}

			return guessAverage;
			}

		static private Fraction ChooseThirdGuess(List<Equation> a_answers)
			{
			Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);

			Fraction bestAverage = 9999;
			foreach(Equation guess in a_answers)
				{
				foreach(Equation twin in guess.m_twins)
					{
					Fraction guessAverage = CalculateThirdGuessAverage(a_answers, twin);
					if (guessAverage < bestAverage)
						{
						bestAverage = guessAverage;
//						MyTrace.WriteLine("Third answer twin guess {0} gave an average of {1}", twin, (decimal) guessAverage);
						}

					if (bestAverage == theoreticalBest)
						{
//						MyTrace.WriteLine("This is the best theoretical average");
						return bestAverage;
						}
					}
				}

			if (bestAverage <= 2)
				{
//				MyTrace.WriteLine("Now that we have tried all answers (and twins), the best we can hope for is an average of 2");
				return bestAverage;
				}

			List<Equation>[] counts = Hint.ListBucketCount(a_answers);

			for(int i=counts.Length-1; i>=0; i--)
				{
				if (counts[i] == null)
					continue;

				foreach(Equation guess in counts[i])
					{
					DebugEx.Assert(guess != null);

					if (guess.m_display == "60/6-1=9")
						DebugEx.Nop();

					Fraction guessAverage = CalculateThirdGuessAverage(a_answers, guess);
					DebugEx.Assert(guessAverage >= 2);

					if (guessAverage < bestAverage)
						{
						bestAverage = guessAverage;
//						MyTrace.WriteLine("Third: Guess {0} gave an average of {1}", guess, (decimal) guessAverage);
						}

					if (bestAverage == 2)
						{
//						MyTrace.WriteLine("This is the best non-answer average");
						return bestAverage;
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
		}
	}
