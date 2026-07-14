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
		static private CcDictionary<string, Fraction> s_avgCache = new CcDictionary<string, Fraction>();

		static public void Solve()
			{
			FindBestFirstGuess();

//			List<Equation> firstGuesses = new List<Equation>();
//			firstGuesses.Add(Equation.FromString("43-25=18"));
//			Json json = MakeJson(1, firstGuesses[0], Equation.AllAnswers);
//			Console.WriteLine(json.ToPrettyString());
			}

		static private void FindBestFirstGuess()
			{
			CcHashSet<Equation> done = new CcHashSet<Equation>();
			StreamReader sr = new StreamReader(Path.Combine(Program.Root, "firsts.log"));
			foreach(string line in sr.EnumerateLines())
				{
				string equ = null;
				int gt3 = -1;
				if (line.TryMatch("^(.{8}): (-?[0-9]+)$", ref equ, ref gt3))
					{
					Equation guess = Equation.FromString(equ);
					done.Add(guess);
					}
				}
			sr.Close();

			Lock sw_lock = new Lock();
			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "firsts.log"), true, Encoding.ASCII);

			CcList<Equation> todo = new CcList<Equation>(Equation.AllEquations);
			todo = new CcList<Equation>(todo.CloneReverse());
			List<Thread> threads = new List<Thread>();
			for(int i=0; i<Environment.ProcessorCount; i++)
				{
				threads.Add(ThreadEx.Fork(delegate
					{
					while(todo.Count > 0)
						{
						Equation first = todo.RemoveFirst();
						if (done.Contains(first))
							continue;
						int gt3 = TryFirstGuess(first);
						using(sw_lock.WriteLock())
							{
							sw.WriteLine("{0}: {1}", first, gt3);
							sw.Flush();
							}
						}
					}));
				}

			for(int i=0; i<threads.Count; i++)
				threads[i].Join();

			sw.Close();
			}

		static private int TryFirstGuess(Equation a_firstGuess)
			{
			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(Equation.AllAnswers, a_firstGuess);
			DebugEx.Assert(buckets.Count > 1);

			int gt3 = 0;
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
				{
				bucketIndex++;
				MyTrace.WriteLine("{0}: {1} of {2}", a_firstGuess, bucketIndex, buckets.Count);
				gt3 += CalculateGt3(2, pair.Value);
				}

			return gt3;
			}

		static private List<string> EquationListToJson(List<Equation> a_equations)
			{
			List<string> displays = new List<string>();
			foreach(Equation equ in a_equations)
				displays.Add(equ.m_display);
			return displays;
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

		static private int CalculateGt3(int a_guessNumber, List<Equation> a_answerList)
			{
			if (a_guessNumber == 1)
				{
				throw new InconceivableException();
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
					if (buckets.Count == 1 && buckets.ContainsKey(0xFF00) == false)
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
