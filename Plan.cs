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
	internal class Plan
		{
		static private Dictionary<string, Fraction> s_avgCache = new Dictionary<string, Fraction>();

		static public void Solve()
			{
			List<Equation> firstGuesses = new List<Equation>();
			firstGuesses.Add(Equation.FromString("43-25=18"));
			Json json = MakeJson(1, firstGuesses[0], Equation.AllAnswers);
			Console.WriteLine(json.ToPrettyString());

//			int gt3 = CalculateBestFirstGuessGt3(firstGuesses);
			}

		static public void Solve(string a_guess)
			{
			Equation guess = Equation.FromString(a_guess);
			Json json = GenerateJson(1, Equation.AllAnswers, guess);
			File.WriteAllText(Path.Combine(Program.Root, "plan.json"), json.ToPrettyString());
			}

		static private List<string> EquationListToJson(List<Equation> a_equations)
			{
			List<string> displays = new List<string>();
			foreach(Equation equ in a_equations)
				displays.Add(equ.m_display);
			return displays;
			}

		static private Json GenerateJson(int a_guessNumber, List<Equation> a_answerList, Equation a_guess)
			{
			Json json = Json.New();

			List<string> answers = new List<string>();
			foreach(Equation equ in a_answerList)
				answers.Add(equ.m_display);
			json["PossibleAnswers"] = EquationListToJson(a_answerList);
			json["PossibleAnswerCount"] = a_answerList.Count;
			json["Guess"] = a_guess.m_display;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, a_guess);
			List<ushort> keys = buckets.Keys.ToList();
			keys.Sort(delegate(ushort a, ushort b) { return Hint.Compare(a, b); });
			Json hints = Json.New();
			int bucketIndex = 0;
			foreach(ushort key in keys)
				{
				List<Equation> value = buckets.GetValue(key);

				if (a_guessNumber == 1)
					MyTrace.WriteLine("Guess {0}: Bucket {1} of {2}: {3} with {4} answers...", a_guess, bucketIndex, buckets.Count, Hint.ToColorString(key), value.Count);
				bucketIndex++;

				if (key == 0xFF00)
					{
					Json answer = Json.New();
					answer["Success"] = true;
					hints[Hint.ToColorString(key)] = answer;
					}
				else
					{
					Equation nextGuess = FindBestGuess(a_guessNumber + 1, value);
					hints[Hint.ToColorString(key)] = GenerateJson(a_guessNumber + 1, value, nextGuess);
					}
				}
			json["Hints"] = hints;

			if (DebugEx.False)
				{
				MyTrace.WriteLine("-------------------------------------------------------------------------------");
				MyTrace.WriteLine(json.ToPrettyString());
				MyTrace.WriteLine("-------------------------------------------------------------------------------");
				}
			return json;
			}

		static private Equation FindBestGuess(int a_guessNumber, List<Equation> a_answerList)
			{
			if (Equation.AreAllTwins(a_answerList))
				return a_answerList[0];
			if (a_answerList.Count == 2)
				return a_answerList[0];

			int bestGt3 = int.MaxValue;
			List<Equation> bestGuesses = new List<Equation>();
			foreach(Equation guess in Equation.AllEquations)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
				if (buckets.Count == 1)
					// This guess did not split our a_answerList at all. It made no progress toward finding the solution.
					continue;

				int gt3 = 0;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					{
					gt3 += CalculateGt3(a_guessNumber + 1, pair.Value);
					if (gt3 > bestGt3)
						break;
					}
				if (gt3 < bestGt3)
					{
					bestGt3 = gt3;
					bestGuesses.Clear();
					}
				if (gt3 == bestGt3)
					bestGuesses.Add(guess);
				if (bestGt3 == 0)
					break;
				}
			DebugEx.Assert(bestGuesses.Count > 0);
			DebugEx.Assert(bestGt3 <= a_answerList.Count);
			return bestGuesses.PickRandom();
			}

		static private int CalculateGt3(int a_guessNumber, List<Equation> a_answerList)
			{
			if (a_guessNumber == 1)
				{
				DebugEx.SelfHalt();
				return int.MaxValue;
				}
			else if (a_guessNumber == 2)
				{
				if (a_answerList.Count == 1)
					return 0;
				if (a_answerList.Count == 2)
					return 0;

				int bestGt3 = int.MaxValue;
				foreach(Equation guess in Equation.AllEquations)
					{
					// This is guess number 3

					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
					if (buckets.Count == 1)
						continue;

					int gt3 = 0;
					foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
						{
						gt3 += CalculateGt3(a_guessNumber + 1, pair.Value);
						if (gt3 >= bestGt3)
							break;
						}
					if (gt3 < bestGt3)
						bestGt3 = gt3;
					if (bestGt3 == 0)
						break;
					}
				return bestGt3;
				}
			else if (a_guessNumber == 3)
				{
				if (Equation.AreAllTwins(a_answerList))
					return 0;
				if (a_answerList.Count == 2)
					return 1;
				return a_answerList.Count - Equation.LargestTwinSetWithin(a_answerList);
				}
			else
				return a_answerList.Count;
			}

		static private int CalculateBestFirstGuessGt3(List<Equation> o_bestFirstGuesses)
			{
			o_bestFirstGuesses.Clear();
			int bestGt3 = int.MaxValue;
			foreach(Equation guess in Equation.AllEquations)
				{
				int gt3 = CalculateFirstGuessGt3(guess, Equation.AllAnswers, bestGt3);
				if (gt3 < bestGt3)
					{
					o_bestFirstGuesses.Clear();
					bestGt3 = gt3;
					}
				if (gt3 == bestGt3)
					o_bestFirstGuesses.Add(guess);
				DebugEx.Assert(bestGt3 >= 0);
				}
			DebugEx.Assert(bestGt3 >= 0);
			return bestGt3;
			}

		static private int CalculateFirstGuessGt3(Equation a_firstGuess, List<Equation> a_answerList, int a_gt3ToBeat)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, a_firstGuess);
			if (buckets.Count == 1)
				// This guess did not split our a_answerList at all. It made no progress toward finding the solution.
				return int.MaxValue;

			List<Equation> bestSecondGuesses = new List<Equation>();
			int gt3 = 0;
			int count = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
				{
				count++;
				MyTrace.WriteLine("{0}: {1} of {2}", a_firstGuess, count, buckets.Count());

				gt3 += CalculateBestSecondGuessGt3(a_firstGuess, pair.Value, bestSecondGuesses);
				DebugEx.Assert(gt3 >= 0);
				if (gt3 >= a_gt3ToBeat)
					break;
				}
			DebugEx.Assert(gt3 >= 0);
			return gt3;
			}

		static private int CalculateBestSecondGuessGt3(Equation a_firstGuess, List<Equation> a_answerList, List<Equation> o_bestSecondGuesses)
			{
			o_bestSecondGuesses.Clear();
			if (a_answerList.Count == 1)
				return 0;

			int bestGt3 = int.MaxValue;
			foreach(Equation guess in Equation.AllEquations)
				{
				int gt3 = CalculateSecondGuessGt3(a_firstGuess, guess, a_answerList, bestGt3);
				if (gt3 < bestGt3)
					{
					o_bestSecondGuesses.Clear();
					bestGt3 = gt3;
					}
				if (gt3 == bestGt3)
					o_bestSecondGuesses.Add(guess);
				}
			DebugEx.Assert(bestGt3 >= 0);
			DebugEx.Assert(bestGt3 < int.MaxValue);
			return bestGt3;
			}

		static private int CalculateSecondGuessGt3(Equation a_firstGuess, Equation a_secondGuess, List<Equation> a_answerList, int a_gt3ToBeat)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, a_secondGuess);
			if (buckets.Count == 1)
				// This guess did not split our a_answerList at all. It made no progress toward finding the solution.
				return int.MaxValue;

			int gt3 = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
				{
				gt3 += CalculateBestThirdGuessGt3(a_firstGuess, a_secondGuess, pair.Value);
				if (gt3 >= a_gt3ToBeat)
					break;
				}
			DebugEx.Assert(gt3 >= 0);
			return gt3;
			}

		static private int CalculateBestThirdGuessGt3(Equation a_firstGuess, Equation a_secondGuess, List<Equation> a_answerList)
			{
			int gt3 = a_answerList.Count - Equation.LargestTwinSetWithin(a_answerList);
			DebugEx.Assert(gt3 >= 0);
			return gt3;
			}

		static private int CalculateBestThirdGuessGt3(Equation a_firstGuess, Equation a_secondGuess, List<Equation> a_answerList, List<Equation> o_bestThirdGuesses)
			{
			o_bestThirdGuesses.Clear();

			MultiDict<int, Equation> twins = new MultiDict<int, Equation>();
			foreach(Equation equ in a_answerList)
				twins.Add(equ.m_twinSetId, equ);
			List<int> largestTwinIds = new List<int>();
			int largestTwinCount = 0;
			foreach(KeyValuePair<int, HashSet<Equation>> pair in twins.KeyValuePairs)
				{
				if (pair.Value.Count > largestTwinCount)
					{
					largestTwinIds.Clear();
					largestTwinCount = pair.Value.Count;
					}
				if (pair.Value.Count == largestTwinCount)
					{
					largestTwinIds.Add(pair.Key);
					}
				}
			foreach(int twinId in largestTwinIds)
				o_bestThirdGuesses.AddRange(twins[twinId]);

			int gt3 = a_answerList.Count - largestTwinCount;
			DebugEx.Assert(gt3 >= 0);
			return gt3;
			}

		static private Json MakeJson(int a_guessNumber, Equation a_guess, List<Equation> a_answerList)
			{
			Json json = Json.New();
			json["PossibleAnswers"] = EquationListToJson(a_answerList);
			json["PossibleAnswerCount"] = a_answerList.Count;
//			json["PossibleGuesses"] = EquationListToJson(a_guesses);
//			Equation guess = a_guesses.PickRandom();
			json["Guess"] = a_guess.m_display;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, a_guess);
			List<ushort> bucketHints = buckets.Keys.ToList();
			bucketHints.Sort(Hint.Compare);
			Json hintsJson = Json.New();
			int bucketIndex = 0;
			foreach(ushort bucketHint in bucketHints)
				{
				List<Equation> bucketAnswerList = buckets.GetValue(bucketHint);

				if (a_guessNumber == 1)
					MyTrace.WriteLine("{0}: Guess {1}: Bucket {2} of {3}: {4} with {5} answers...", a_guessNumber, a_guess, bucketIndex, buckets.Count, Hint.ToColorString(bucketHint), bucketAnswerList.Count);
				bucketIndex++;

				if (bucketHint == 0xFF00)
					{
					Json answer = Json.New();
					answer["Success"] = true;
					hintsJson[Hint.ToColorString(bucketHint)] = answer;
					}
				else
					{
					Equation nextGuess = FindBestNextGuess(a_guessNumber + 1, bucketAnswerList);
					hintsJson[Hint.ToColorString(bucketHint)] = MakeJson(a_guessNumber + 1, nextGuess, bucketAnswerList);
					}
				}
			json["Hints"] = hintsJson;

			if (DebugEx.False)
				{
				MyTrace.WriteLine("-------------------------------------------------------------------------------");
				MyTrace.WriteLine(json.ToPrettyString());
				MyTrace.WriteLine("-------------------------------------------------------------------------------");
				}

			return json;
			}

		static private Equation FindBestNextGuess(int a_guessNumber, List<Equation> a_answerList)
			{
			if (a_answerList.Count == 1)
				return a_answerList[0];

			if (a_guessNumber >= 3)
				DebugEx.Nop();

			DebugEx.Assert(a_guessNumber > 0);
//			if (a_guessNumber == 1 || a_guessNumber == 2)
			if (true)
				{
				//
				// Find the guesses that give us the lowest GT3.
				//
				List<Equation> best = new List<Equation>();
				int bestGt3 = int.MaxValue;
				foreach(Equation guess in Equation.AllEquations)
					{
					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
					if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
						continue;

					int gt3 = 0;
					foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
						{
						gt3 += CalculateGt3(a_guessNumber + 1, pair.Value);
						if (gt3 > bestGt3)
							break;
						}

					if (gt3 < bestGt3)
						{
						best.Clear();
						bestGt3 = gt3;
						}
					if (gt3 == bestGt3)
						best.Add(guess);
					}

				if (best.Count == 1)
					return best[0];

				//
				// Since we have multiple candidates, pick the one with the best AVG.
				//
				List<Equation> bestOfBest = new List<Equation>();
				Fraction bestAvg = Fraction.MaxValue;
				foreach(Equation guess in best)
					{
					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
					if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
						continue;

					Fraction avg = 1;
					foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
						{
						if (pair.Key == 0xFF00)
							continue;

						Fraction bucketWeight = ((Fraction) pair.Value.Count / a_answerList.Count);
						Fraction bucketAvg = CalculateAvg(pair.Value);
						avg += bucketAvg * bucketWeight;
						if (avg > bestAvg)
							break;
						}

					if (avg < bestAvg)
						{
						bestOfBest.Clear();
						bestAvg = avg;
						}
					if (avg == bestAvg)
						bestOfBest.Add(guess);
					}

				DebugEx.Assert(bestOfBest.Count > 0);
				if (bestOfBest.Count == 1)
					return bestOfBest[0];

				//
				// Since we still have more than one candidate, just pick one
				//
//				foreach(Equation guess in bestOfBest)
//					DebugEx.Assert(a_answerList.Contains(guess));
				bestOfBest.Sort(Equation.Compare);
				return bestOfBest[0];
				}
			}

		static private Fraction CalculateAvg(List<Equation> a_answerList)
			{
			if (a_answerList.Count == 1)
				return 1;

			if (Equation.AreAllTwins(a_answerList))
				return 1;

			if (a_answerList.Count == 2)
				return (Fraction) 1.5;

			Fraction bestAvg;

			string hash = Equation.HashAnswerList(a_answerList).ToHex();
			if (s_avgCache.TryGetValue(hash, out bestAvg))
				return bestAvg;

			bestAvg = Fraction.MaxValue;
			foreach(Equation guess in a_answerList)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;

				Fraction avg = 1;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					{
					Fraction bucketWeight = ((Fraction) pair.Value.Count / a_answerList.Count);
					Fraction bucketAvg = CalculateAvg(pair.Value);
					avg += bucketAvg * bucketWeight;
					if (avg > bestAvg)
						break;
					}

				if (avg < bestAvg)
					bestAvg = avg;
				}

			if (bestAvg <= 2)
				{
				s_avgCache.Add(hash, bestAvg);
				return bestAvg;
				}

			foreach(Equation guess in Equation.AllEquations)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
				if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
					continue;

				Fraction avg = 1;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					{
					Fraction bucketWeight = ((Fraction) pair.Value.Count / a_answerList.Count);
					Fraction bucketAvg = CalculateAvg(pair.Value);
					avg += bucketAvg * bucketWeight;
					if (avg > bestAvg)
						break;
					}

				if (avg < bestAvg)
					bestAvg = avg;
				}

			s_avgCache.Add(hash, bestAvg);
			return bestAvg;
			}
		}
	}
