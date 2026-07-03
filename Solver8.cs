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
	internal class Solver8
		{
		private int[] m_usedHints = new int[65536];

		static public void Solve()
			{
			MyTrace.WriteLine("Solver8 working...");

			Solver8 me = new Solver8();
//			me.WorkerThread();
//			me.MakePlan("54-38=16");
			me.DrawPlan(Json.Parse(File.ReadAllText(Path.Combine(Program.Root, "plan.json"))), Path.Combine(Program.Root, "plan.html"));
			}

		public Solver8()
			{
			}

		private void WorkerThread()
			{
			Equation guess;
			Fraction average;
			FindBestGuess(0, Equation.AllAnswers, out guess, out average);
			MyTrace.WriteLine("Guess = {0}", guess);
			MyTrace.WriteLine("Average = {0}", average);
			}

		private void FindBestGuess(int a_depth, List<Equation> a_answers, out Equation o_guess, out Fraction o_average)
			{
			o_guess = null;
			o_average = 9999;

			if (a_answers.Count < Equation.AllAnswers.Count)
				{
				DateTime nextPrint = DateTime.UtcNow.AddSeconds(60);

				//
				// Try all the equations that are part of a_answers (or their twins)
				//
				Fraction theoreticalBest = Equation.CalculateTheoreticalBestAverage(a_answers);
				int answerIndex = 0;
				foreach(Equation answer in a_answers)
					{
					answerIndex++;

					foreach(Equation guess in answer.m_twins)
						{
						Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
						if (buckets.Count == 1)
							{
							if (buckets.ContainsKey(0xFF00))
								{
								o_guess = guess;
								o_average = 1;
								return;
								}
							continue;
							}

						Fraction guessAverage = 1;
						int bucketIndex = 0;
						foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
							{
							if (DateTime.UtcNow > nextPrint)
								{
								MyTrace.WriteLine("Depth {0}: Answer {1}: Bucket {2} of {3}", a_depth, answerIndex - 1, bucketIndex, buckets.Count);
								nextPrint = DateTime.UtcNow.AddSeconds(60);
								}

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
								FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
								}

							guessAverage += bucketAverage * bucketFraction;
							bucketIndex++;
							}

						if (guessAverage < o_average)
							{
							o_guess = guess;
							o_average = guessAverage;

							DebugEx.Assert(guessAverage >= theoreticalBest);
							if (guessAverage == theoreticalBest)
								return;
							}
						}
					}

				if (o_average <= 2)
					return;
						
				//
				// Do a fast pass through all the equations looking for the optimal average of 2.
				//
				Equation twoGuess = Hint.FindAverageOfTwo(a_answers);
				if (twoGuess != null)
					{
					o_guess = twoGuess;
					o_average = 2;
					return;
					}

				//
				// Try every guess, starting with the ones with most number of buckets.
				//
				List<Equation>[] bucketCounts = Hint.ListBucketCount(a_answers);

				decimal gapSum = 0;

				for(int iSize=bucketCounts.Length-1; iSize >= 1; iSize--)
					{
					if (gapSum > 25)
						break;

					List<Equation> equations = bucketCounts[iSize];
					if (equations == null)
						continue;

					for(int iEqu=0; iEqu < equations.Count; iEqu++)
						{
						Equation guess = equations[iEqu];

						Dictionary<ushort,List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);
						DebugEx.Assert(buckets.Count == iSize);

						Fraction guessAverage = 1;
						int bucketIndex = 0;
						foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
							{
							if (DateTime.UtcNow > nextPrint)
								{
								MyTrace.WriteLine("Depth {0}: Guess size {1}, {2} of {3}: Bucket {4} of {5}", a_depth, iSize, iEqu, equations.Count, bucketIndex, buckets.Count);
								nextPrint = DateTime.UtcNow.AddSeconds(60);
								}

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
								FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
								}

							guessAverage += bucketAverage * bucketFraction;
							bucketIndex++;
							}

						DebugEx.Assert(guessAverage >= 2);

						if (guessAverage < o_average)
							{
							o_guess = guess;
							o_average = guessAverage;
							gapSum = 0;
							}
						else
							{
							decimal gap = guessAverage - o_average;
							gapSum = gapSum * (decimal) 0.99;
							gapSum += gap;
							}
						}
					}
				DebugEx.Assert(o_average < 9999);
				return;
				}
			else
				{
				//
				// This is the first guess, we simply try all possible equations
				//
				HashSet<string> done = new HashSet<string>();
				foreach(CsvRow row in CsvFile.ReadRows(Path.Combine(Program.Root, "firsts.csv")))
					done.Add(row["Guess"]);

				StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "firsts.csv"), true);
				CsvHeader header = new CsvHeader("Guess,Buckets,Average");

				List<Equation> guesses = Equation.AllEquations;
//				List<Equation> guesses = Equation.AllEquations.Clone().Randomize();
//				List<Equation> guesses = new List<Equation>();
//				guesses.Add(Equation.FromString("9+4*-2=1"));

				DebugEx.Assert(a_depth == 0);
				for(int iEqu=0; iEqu<guesses.Count; iEqu++)
					{
					Equation guess = guesses[iEqu];

					if (done.Contains(guess.ToString()))
						continue;

					MyTrace.WriteLine("Processing {0}...", guess);

					Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answers, guess);

					if (buckets.Count == 1)
						{
						if (buckets.ContainsKey(0xFF00))
							{
							DebugEx.SelfHalt();
							o_guess = guess;
							o_average = 1;
							return;
							}
						continue;
						}

					List<KeyValuePair<ushort, List<Equation>>> pairs = buckets.ToList();
					pairs.Sort(delegate(KeyValuePair<ushort, List<Equation>> a, KeyValuePair<ushort, List<Equation>> b)
						{
						return Int32Ex.Compare(b.Value.Count, a.Value.Count);
						});

					Fraction guessAverage = 1;
					int bucketIndex = 0;
					foreach(KeyValuePair<ushort, List<Equation>> pair in pairs)
						{
						MyTrace.WriteLine("Depth {0}: Guess {1} of {2}: Bucket {3} of {4} with {5} answers", a_depth, iEqu, guesses.Count, bucketIndex, pairs.Count, pair.Value.Count);
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
							FindBestGuess(a_depth+1, pair.Value, out bucketGuess, out bucketAverage);
							}

						guessAverage += bucketAverage * bucketFraction;
						bucketIndex++;
						}

					DebugEx.Assert(guessAverage >= 2);

					if (guessAverage < o_average)
						{
						o_guess = guess;
						o_average = guessAverage;
						}

					CsvRow row = new CsvRow(header);
					row["Guess"] = guess.ToString();
					row["Buckets"] = buckets.Count.ToString();
					row["Average"] = guessAverage.ToString();
					sw.WriteLine(row.ToString());
					sw.Flush();
					}
				DebugEx.Assert(o_average < 9999);
				sw.Close();
				return;
				}
			}

		class Plan
			{
			internal List<Equation> m_answers;
			internal Equation m_guess;
			internal Fraction m_average;
			internal Dictionary<ushort,Plan> m_hints;

			internal Plan()								{ }
			internal Plan(List<Equation> a_answers)		{ m_answers = a_answers; }
			public override string ToString()
				{
				return String.Format("Plan for {0} answers split into {1} hints", m_answers.Count, m_hints == null ? 0 : m_hints.Count);
				}

			internal Json ToJson()
				{
				Json json = Json.New();
				List<string> answerStrings = new List<string>();
				foreach(Equation equ in m_answers)
					answerStrings.Add(equ.ToString());
				json["Answers"] = answerStrings;
				if (m_guess != null)
					json["Guess"] = m_guess.ToString();
				if (m_average != null)
					json["Average"] = m_average.ToString();
				if (m_hints != null)
					{
					Json hints = Json.New();
					foreach(KeyValuePair<ushort,Plan> pair in m_hints)
						hints[pair.Key.ToString()] = pair.Value.ToJson();
					json["Hints"] = hints;
					}
				return json;
				}
			}

		private void MakePlans()
			{
			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "plans.json"));

			foreach(Equation guess in Equation.AllEquations)
				{
				Plan plan = new Plan(Equation.AllAnswers);
				plan.m_guess = guess;
				MakePlan(0, plan);

				Json json = plan.ToJson();
				Console.WriteLine(json.ToPrettyString());
				sw.WriteLine(json.ToCompactString());
				sw.Flush();
				}

			sw.Close();
			}

		private void MakePlan(string a_equation)
			{
			Plan plan = new Plan(Equation.AllAnswers);
			plan.m_guess = Equation.FromString(a_equation);
			MakePlan(0, plan);

			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "plan.json"));
			Json json = plan.ToJson();
			Console.WriteLine(json.ToPrettyString());
			sw.WriteLine(json.ToCompactString());
			sw.Close();
			}

		private void MakePlan(int a_depth, Plan a_plan)
			{
			DateTime nextPrint = DateTime.UtcNow.AddSeconds(60);

			if (a_plan.m_guess == null)
				FindBestGuess(a_depth, a_plan.m_answers, out a_plan.m_guess, out a_plan.m_average);

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_plan.m_answers, a_plan.m_guess);
			if (buckets.Count == 1)
				{
				DebugEx.Assert(buckets.ContainsKey(0xFF00));
				return;
				}

			a_plan.m_hints = new Dictionary<ushort, Plan>();
			int bucketIndex = 0;
			foreach(KeyValuePair<ushort,List<Equation>> pair in buckets)
				{
				if (DateTime.UtcNow > nextPrint)
					{
					MyTrace.WriteLine("Depth {0}: Listing bucket {1} of {2}", a_depth, bucketIndex, buckets.Count);
					nextPrint = DateTime.UtcNow.AddSeconds(60);
					}

				Plan subPlan = new Plan();
				subPlan.m_answers = pair.Value;
				a_plan.m_hints[pair.Key] = subPlan;
				bucketIndex++;
				}

			bucketIndex = 0;
			foreach(KeyValuePair<ushort,Plan> pair in a_plan.m_hints)
				{
				bucketIndex++;

				if (pair.Key == 0xFF00)
					continue;

				if (DateTime.UtcNow > nextPrint)
					{
					MyTrace.WriteLine("Depth {0}: Planning bucket {1} of {2}", a_depth, bucketIndex - 1, buckets.Count);
					nextPrint = DateTime.UtcNow.AddSeconds(60);
					}

				MakePlan(a_depth+1, pair.Value);
				}

			if (a_depth == 1)
				DebugEx.Nop();
			}

		private void DrawPlan(Json a_json, string a_htmlFile)
			{
			StreamWriter sw = new StreamWriter(a_htmlFile);

			DrawHintTable(0, sw, a_json);

			sw.Close();
			}

		private void DrawHintTable(int a_depth, StreamWriter a_sw, Json a_json)
			{
			string borderColor = "black";
			if (a_depth == 0)
				borderColor = "red";
			if (a_depth == 1)
				borderColor = "blue";
			if (a_depth == 2)
				borderColor = "green";
			if (a_depth == 3)
				borderColor = "yellow";

			if (a_json["Hints"] == null)
				{
				Equation guess;
				if (a_json["Guess"] != null)
					guess = Equation.FromString(a_json["Guess"]);
				else
					guess = Equation.FromString(a_json["Answers", 0]);
				a_sw.WriteLine(Hint.ToHtml(0xFF00, guess));
				}
			else
				{
				a_sw.WriteLine("<TABLE BORDER=1 CELLPADDING=2 CELLSPACING=0 BORDERCOLOR={0}>", borderColor);

				Equation guess = Equation.FromString(a_json["Guess"]);
				List<string> hints = a_json["Hints"].Members.Keys.ToList();
				hints.Sort(delegate(string a, string b) { return Hint.Compare(a.ToUshort(), b.ToUshort()); });

				foreach(string hint in hints)
					{
					a_sw.WriteLine("<TR>");
					a_sw.WriteLine("<TD VALIGN=top>{0}</TD>", Hint.ToHtml(hint.ToUshort(), guess));
					a_sw.WriteLine("<TD>");
					if (hint.ToUshort() == 0xFF00)
						a_sw.WriteLine("&nbsp;");
					else
						DrawHintTable(a_depth+1, a_sw, a_json["Hints", hint]);
					a_sw.WriteLine("</TD>");
					a_sw.WriteLine("</TR>");
					}

				a_sw.WriteLine("</TABLE>");
				}
			}
		}
	}
		