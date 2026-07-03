using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Patrick;
using Patrick.Csv2;

namespace Nerdle
	{

	// https://nerdschalk.com/best-nerdle-start-numbers-and-equations/
	// https://medium.com/@duanemay/nerdle-strategies-59b3a2fad18a

	static class Program
		{
		static public string Root = @"G:\Projects\Nerdle";

		static void Main(string[] args)
			{
			MyTrace.UseConsole();

			Console.WriteLine("Let's Nerdle!...");
			DebugEx.Assert(Directory.Exists(Program.Root));

//			Equation.BuildLists();
//			Equation.Initialize();
			Equation.LoadEquations();

//			Hint.CreateTableFile();
			Hint.OpenTable();
//			GreaterThanThree.Solve();
//			Plan.Solve("43-25=18");
//			GreaterThanThree.SolveFirstGuess("43-25=18");
			Plan.Solve();
			Hint.CloseTable();

			Console.WriteLine("And... we're done.");
			}

		static private void PrintBasicStatistics()
			{
			Console.WriteLine("Total number of valid answers = {0}", Equation.AllAnswerStrings.Count);
			Console.WriteLine("Total number of valid guesses = {0}", Equation.AllGuessStrings.Count);
			Console.WriteLine("Total number of twin sets = {0}", Equation.Twins.Count);
			}
		}
	}


/*
		//													  0001 0002 0004 0008 0010 0020 0040 0080 0100 0200 0400 0800 1000 2000 4000  
		static public char[] s_chars = new char[]			{ '=', '/', '*', '+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
//		static public Dictionary<char, ushort> s_char2ushort;
		static public ushort[] s_char2ushort;
		static public ushort s_opMask;
		static public ushort s_equalSign;
		static public ushort s_opeqMask;
		static public int[] s_char2index = new int[256];

		static public Lock s_mutex = new Lock();
//		static public List<string> s_answers = new List<string>();
//		static public List<string> s_guesses = new List<string>();
		static public HashSet<string> s_answers = new HashSet<string>();
		static public HashSet<string> s_guesses = new HashSet<string>();
		static public Dictionary<string,CsvRow> s_singles = new Dictionary<string, CsvRow>();
		static public Dictionary<Tuple<string,string>,CsvRow> s_doubles = new Dictionary<Tuple<string, string>, CsvRow>();


		static void Main(string[] args)
			{
			Console.WriteLine("Let's Nerdle!...");
			DebugEx.Assert(Directory.Exists(Program.Root));

			PrepareStatics();

//			Console.WriteLine("Generating answers.txt...");
//			AnswerEnumerator.SaveAnswerList();

//			Console.WriteLine("Generating guesses.txt...");
//			GuessEnumerator.SaveGuessList();

			Console.WriteLine("Loading answers.txt...");
			s_answers.AddRange(File.ReadAllLines(Path.Combine(Program.Root, "answers.txt")));

			Console.WriteLine("Loading guesses.txt...");
			s_guesses.AddRange(File.ReadAllLines(Path.Combine(Program.Root, "guesses.txt")));

//			Console.WriteLine("Generating singles.csv...");
//			SaveAllSingleStarters();

//			Console.WriteLine("Loading singles.csv...");
//			foreach(CsvRow csv in CsvFile.ReadRows(Path.Combine(Program.Root, "singles.csv")))
//				s_singles.Add(csv["Guess"], csv);

//			Console.WriteLine("Loading doubles.csv...");
//			foreach(CsvRow csv in CsvFile.ReadRows(Path.Combine(Program.Root, "doubles.csv")))
//				s_doubles.Add(new Tuple<string,string>(csv["Guess1"], csv["Guess2"]), csv);

//			Console.WriteLine("Generating doubles.csv...");
//			SaveAllDoubleStarters();

//			FindBestApproach();
//			FindMyBestPairs();
//			SortByOperatorPlacement();
//			PrintLeastOverlapDoubles();
//			FindBestSingleStarter();
//			EnumOperationPositions();
//			PrintDistributionTable();
//			PrintMaxRepeats();
//			PrintMyBestStarter();

			Test();
			}

		static void PrepareStatics()
			{
			Console.WriteLine("Preparing char map...");
//			s_char2ushort = new Dictionary<char, ushort>();
			s_char2ushort = new ushort[256];
			for(int i=0; i<s_chars.Length; i++)
				{
				s_char2ushort[s_chars[i]] = (ushort) (1 << i);
				s_char2index[s_chars[i]] = i;
				}
			s_opMask = (ushort) (s_char2ushort['/'] | s_char2ushort['*'] | s_char2ushort['+'] | s_char2ushort['-']);
			s_equalSign = s_char2ushort['='];
			s_opeqMask = (ushort) (s_opMask | s_equalSign);
			}

		static void AddSingle(string a_guess, double a_prob)
			{
			using(s_mutex.WriteLock())
				{
				CsvRow csv;
				if (s_singles.TryGetValue(a_guess, out csv) == false)
					{
					csv = new CsvRow();
					csv["Guess"] = a_guess;
					s_singles.Add(a_guess, csv);
					}
				csv["Probability"] = a_prob.ToString();
				}
			}

		static void SaveSingles()
			{
			using(s_mutex.WriteLock())
				{
				Console.WriteLine("Saving singles.csv...");
				List<CsvRow> rows = new List<CsvRow>(s_singles.Values);
				rows.Sort(delegate(CsvRow a, CsvRow b)
					{
					int cmp = DoubleEx.Compare(b["Probability"].ToDouble(), a["Probability"].ToDouble());
					if (cmp == 0)
						cmp = String.Compare(a["Guess"], b["Guess"]);
					return cmp;
					});
				CsvFile.WriteRows(Path.Combine(Program.Root, "singles.csv"), rows);
				}
			}

		static void AddDouble(string a_guess1, string a_guess2, double a_prob)
			{
			using(s_mutex.WriteLock())
				{
				Tuple<string,string> tuple = new Tuple<string, string>(a_guess1, a_guess2);
				CsvRow csv;
				if (s_doubles.TryGetValue(tuple, out csv) == false)
					{
					csv = new CsvRow();
					csv["Guess1"] = a_guess1;
					csv["Guess2"] = a_guess2;
					s_doubles.Add(tuple, csv);
					}
				csv["Probability"] = a_prob.ToString();
				}
			}

		static void SaveDoubles()
			{
			using(s_mutex.WriteLock())
				{
				Console.WriteLine("Saving doubles.csv...");
				List<CsvRow> rows = new List<CsvRow>(s_doubles.Values);
				rows.Sort(delegate(CsvRow a, CsvRow b)
					{
					int cmp = DoubleEx.Compare(b["Probability"].ToDouble(), a["Probability"].ToDouble());
					if (cmp == 0)
						cmp = String.Compare(a["Guess1"], b["Guess1"]);
					if (cmp == 0)
						cmp = String.Compare(a["Guess2"], b["Guess2"]);
					return cmp;
					});
				CsvFile.WriteRows(Path.Combine(Program.Root, "doubles.csv"), rows);
				}
			}

		static void FindBestSingleStarter()
			{
			List<string> allGuesses = new List<string>(File.ReadAllLines(Path.Combine(Program.Root, "guesses.txt")));
			Dictionary<string,double> results = new Dictionary<string, double>();
			
			AnswerList al = new AnswerList();

			foreach(string guess in allGuesses.Randomize())
				{
				Stopwatch sw = new Stopwatch();
				sw.Start();
				double prob = al.CalculateRandomProbability(new string[] { guess });
				sw.Stop();
				Console.WriteLine("guess = {0}, prob = {1:0.00%}, time = {2}ms", guess, prob, sw.ElapsedMilliseconds);
				results.Add(guess, prob);
				}

			List<string> sorted = new List<string>(allGuesses);
			sorted.Sort(delegate(string a, string b) { return DoubleEx.Compare(results[a], results[b]); });

			Console.WriteLine("----- sorted -----");
			foreach(string guess in sorted)
				{
				Console.WriteLine("{0} = {1}%", guess, results[guess] * 100);
				}
			}

		static void EnumOperationPositions()
			{
			List<string> allAnswers = new List<string>(File.ReadAllLines(Path.Combine(Program.Root, "answers.txt")));
			Bag<string> ops = new Bag<string>();
			foreach(string answer in allAnswers)
				{
				string op = answer;
				op = op.ReplaceAll('0', 'n');
				op = op.ReplaceAll('1', 'n');
				op = op.ReplaceAll('2', 'n');
				op = op.ReplaceAll('3', 'n');
				op = op.ReplaceAll('4', 'n');
				op = op.ReplaceAll('5', 'n');
				op = op.ReplaceAll('6', 'n');
				op = op.ReplaceAll('7', 'n');
				op = op.ReplaceAll('8', 'n');
				op = op.ReplaceAll('9', 'n');
				ops.Add(op);
				}

			foreach(string op in ops.Values)
				Console.WriteLine("{0} = {1}", op, ops.CountOf(op));
			}

		static void PrintDistributionTable()
			{
			int[] char2index = new int[256];
			for(int i=0; i<15; i++)
				char2index[Program.s_chars[i]] = i;

			List<string> allAnswers = new List<string>(File.ReadAllLines(Path.Combine(Program.Root, "answers.txt")));
			int[,] table = new int[8,15];

			foreach(string answer in allAnswers)
				{
				for(int i=0; i<8; i++)
					table[i,char2index[answer[i]]]++;
				}

			Console.Write("    ");
			for(int x=0; x<8; x++)
				Console.Write("   {0}    ", x);
			Console.WriteLine();

			for(int y=0; y<15; y++)
				{
				Console.Write(" {0}: ", Program.s_chars[y]);
				for(int x=0; x<8; x++)
					{
					Console.Write(" {0:00.00}% ", (double) table[x,y] * 100 / allAnswers.Count);
					}
				Console.WriteLine();
				}
			}

		static int[,] CalculateDistributionTable()
			{
			int[] char2index = new int[256];
			for(int i=0; i<15; i++)
				char2index[Program.s_chars[i]] = i;

			List<string> allAnswers = new List<string>(File.ReadAllLines(Path.Combine(Program.Root, "answers.txt")));
			int[,] table = new int[8,15];

			foreach(string answer in allAnswers)
				{
				for(int i=0; i<8; i++)
					table[i,char2index[answer[i]]]++;
				}

			return table;
			}

		static private void PrintMaxRepeats()
			{
			int[] char2index = new int[256];
			for(int i=0; i<15; i++)
				char2index[Program.s_chars[i]] = i;

			List<string> allAnswers = new List<string>(File.ReadAllLines(Path.Combine(Program.Root, "guesses.txt")));
			foreach(string answer in allAnswers)
				{
				Bag<char> bag = new Bag<char>();
				foreach(char c in answer)
					bag.Add(c);

				if (bag.CountOf(bag.PickMostPopular()) == 7)
					Console.WriteLine(answer);
				}
			}

		static void SaveAllSingleStarters()
			{
			Console.WriteLine("Preparing todo list...");
			HashSet<string> guessSet = new HashSet<string>(s_guesses);
			guessSet.RemoveRange(s_singles.Keys);
			List<string> guessList = new List<string>(guessSet);

			Lock mutex = new Lock();
			AnswerList al = new AnswerList();

			int index = -1;
			Func<bool> worker = delegate
				{
				int chosen = Interlocked.Increment(ref index);
				if (chosen >= guessList.Count)
					return false;

				string guess = guessList[chosen];
				double prob = al.CalculateRandomProbability(new string[] { guess });
				AddSingle(guess, prob);
				return true;
				};

			Action printer = delegate
				{
				Console.WriteLine("{0} of {1}...", index, guessList.Count);
				};

			Action saver = delegate
				{
				SaveSingles();
				};

//			Parallelize(worker, 16, printer, saver);
			Parallelize(worker, 1, printer, saver);
			}

		static void Parallelize(Func<bool> a_worker, int a_threadCount, Action a_printer, Action a_saver)
			{
			bool stop = false;
			ConsoleEx.AddControlCHandler(delegate { stop = true; });

			Lock mutex = new Lock();
			DateTime nextPrint = DateTime.MinValue;
			DateTime nextSave = DateTime.UtcNow.AddMinutes(60);
			List<Thread> threads = new List<Thread>();
			for(int i=0; i<a_threadCount; i++)
				{
				threads.Add(ThreadEx.Fork(delegate
					{
					while(stop == false)
						{
						if (DateTime.UtcNow > nextPrint)
							{
							using(mutex.WriteLock())
								{
								nextPrint = DateTime.UtcNow.AddSeconds(1);
								a_printer();
								}
							}
						if (a_worker() == false)
							break;
						if (DateTime.UtcNow > nextSave)
							{
							using (mutex.WriteLock())
								{
								nextSave = DateTime.UtcNow.AddMinutes(60);
								a_saver();
								}
							}
						}
					}));
				}
			ConsoleEx.RemoveAllControlCHandlers();

			foreach(Thread thread in threads)
				thread.Join();
			threads.Clear();

			a_saver();
			}

		static void SaveAllDoubleStarters()
			{
			int collisionsWanted = 2;
			List<string> singles = new List<string>();

			Console.WriteLine("Filtering guesses by collisions...");
			foreach(string guess in s_guesses)
//			foreach(string guess in s_answers)
				{
				if (GetCollisionCount(guess) < collisionsWanted)
					singles.Add(guess);
				}

			Console.WriteLine("Generating promising doubles...");
			Lock mutex = new Lock();
			HashSet<Tuple<string,string>> doubles = new HashSet<Tuple<string, string>>();
			int index = -1;
			Func<bool> worker = delegate
				{
				int chosen = Interlocked.Increment(ref index);
				if (chosen >= singles.Count)
					return false;

				string first = singles[chosen];
				for(int i=0; i<singles.Count; i++)
					{
					string second = singles[i];
					if (first == second)
						continue;

					if (GetCollisionCount(first, second) == collisionsWanted)
						{
						using(mutex.WriteLock())
							{
							if (doubles.Contains(new Tuple<string,string>(second, first)) == false)
								doubles.Add(new Tuple<string,string>(first, second));
							}
						}
					}

				return true;
				};

			Action printer = delegate
				{
				Console.WriteLine("Listing {0} of {1}...", index, singles.Count);
				};

			Action saver = delegate
				{
				};

			Parallelize(worker, 16, printer, saver);

			Console.WriteLine("Removing existing doubles...");
			List<Tuple<string,string>> doublesList = new List<Tuple<string, string>>();
			foreach(Tuple<string,string> dbl in doubles)
				{
				if (s_doubles.ContainsKey(dbl) == false)
					doublesList.Add(dbl);
				}
			doublesList = doublesList.RandomizeLarge();

			AnswerList al = new AnswerList();
			index = -1;
			worker = delegate
				{
				int chosen = Interlocked.Increment(ref index);
				if (chosen >= doublesList.Count)
					return false;

				Tuple<string,string> guesses = doublesList[chosen];
				double prob = al.CalculateRandomProbability(new string[] { guesses.Item1, guesses.Item2 });
				AddDouble(guesses.Item1, guesses.Item2, prob);

				return true;
				};

			printer = delegate
				{
				Console.WriteLine("Calculating {0} of {1}...", index, doublesList.Count);
				};

			saver = delegate
				{
				SaveDoubles();
				};

			Parallelize(worker, 16, printer, saver);
			}

		static private int GetCollisionCount(string a_guess)
			{
			ushort used = 0;
			int collisions = 0;

			foreach(char c in a_guess)
				{
				ushort bits = s_char2ushort[c];
				if ((bits & used) != 0)
					collisions++;
				used |= bits;
				}
			return collisions;
			}

		static private int GetCollisionCount(string a_guess1, string a_guess2)
			{
			ushort used = 0;
			int collisions = 0;

			foreach(char c in a_guess1)
				{
				ushort bits = s_char2ushort[c];
				if ((bits & used) != 0)
					collisions++;
				used |= bits;
				}

			foreach(char c in a_guess2)
				{
				ushort bits = s_char2ushort[c];
				if ((bits & used) != 0)
					collisions++;
				used |= bits;
				}

			return collisions;
			}

		static private int GetOverlapCount(string a_first, string a_second)
			{
			ushort firstMap = 0;
			foreach(char c in a_first)
				firstMap |= s_char2ushort[c];

			ushort secondMap = 0;
			foreach(char c in a_second)
				secondMap |= s_char2ushort[c];

			ushort overlap = (ushort) (firstMap & secondMap);
			return UShortEx.BitSetCount(overlap);
			}

		static private int GetOperatorCount(string a_guess)
			{
			ushort map = 0;
			foreach(char c in a_guess)
				map |= s_char2ushort[c];

			map = (ushort) (map & 0x001e);
			return UShortEx.BitSetCount(map);
			}

		static private ushort GetCharMap(string a_guess)
			{
			ushort map = 0;
			foreach(char c in a_guess)
				map |= s_char2ushort[c];
			return map;
			}

		static void PrintMyBestStarter()
			{
			int[,] table = CalculateDistributionTable();

			ushort opMask = (ushort) (s_char2ushort['/'] | s_char2ushort['*'] | s_char2ushort['+'] | s_char2ushort['-']);

			long bestScore = 0;
			string bestGuess = null;
			foreach(string guess in s_guesses)
				{
				ushort map = GetCharMap(guess);
				if (UShortEx.BitSetCount(map) < 8)
					continue;
				if (UShortEx.BitSetCount((ushort) (map & opMask)) < 3)
					continue;

				long score = 0;

				for(int x=0; x<8; x++)
					{
					int y = s_chars.IndexOf(guess[x]);
					if (y >= 0 && y <= 4)
						score += 1000000 * table[x,y];
					if (y >= 5 && y <= 14)
						score += table[x,y];
					}

				if (score > bestScore)
					{
					bestScore = score;
					bestGuess = guess;
					}
				}

			Console.WriteLine("Best first guess: {0}", bestGuess);
			}

		static void FindMyBestPairs()
			{
			int[,] table = CalculateDistributionTable();

			ushort opMask = (ushort) (s_char2ushort['/'] | s_char2ushort['*'] | s_char2ushort['+'] | s_char2ushort['-']);

			List<string> guesses = new List<string>(s_guesses);
			Dictionary<string,long> scores = new Dictionary<string, long>();
			foreach(string guess in guesses)
				{
				ushort map = GetCharMap(guess);
				if (UShortEx.BitSetCount(map) < 8)
					continue;
				if (UShortEx.BitSetCount((ushort) (map & opMask)) < 3)
					continue;

				long score = 0;

				for(int x=0; x<8; x++)
					{
					int y = s_chars.IndexOf(guess[x]);
					if (y >= 0 && y <= 4)
						score += 1000000 * table[x,y];
					if (y >= 5 && y <= 14)
						score += table[x,y];
					}
				scores.Add(guess, score);
				}

			guesses = new List<string>(scores.Keys);
			guesses.Sort(delegate(string a, string b)
				{
				int cmp = Int64Ex.Compare(scores[b], scores[a]);
				if (cmp == 0)
					cmp = String.Compare(a, b);
				return cmp;
				});
			
			foreach(string guess in guesses)
				{
				Console.WriteLine("Chosen first guess: {0}", guess);
				FindMyBestSecond(guess);
				}
			}

		static private ushort GetOverlap(string a_first, string a_second)
			{
			ushort firstMap = 0;
			foreach(char c in a_first)
				firstMap |= Program.s_char2ushort[c];

			ushort secondMap = 0;
			foreach(char c in a_second)
				secondMap |= Program.s_char2ushort[c];

			ushort overlap = (ushort) (firstMap & secondMap);
			return overlap;
			}

		static private Dictionary<ushort, List<string>> GroupInBuckets(string a_first, List<string> a_answers)
			{
			Dictionary<ushort, List<string>> buckets = new Dictionary<ushort, List<string>>();

			ushort opMask = (ushort) (s_char2ushort['/'] | s_char2ushort['*'] | s_char2ushort['+'] | s_char2ushort['-']);
			ushort equalMask = s_char2ushort['='];
			int equalPos = a_first.IndexOf('=');

			for(int i=0; i<a_answers.Count; i++)
				{
				string answer = a_answers[i];

				ushort bucketId = (ushort) (GetOverlap(a_first, answer) & opMask);
				if (answer[equalPos] == '=')
					bucketId |= equalMask;

				List<string> bucket;
				if (buckets.TryGetValue(bucketId, out bucket) == false)
					{
					bucket = new List<string>();
					buckets.Add(bucketId, bucket);
					}
				bucket.Add(answer);
				}

			return buckets;
			}

		static void PrintGuessList(List<string> a_list)
			{
			int cols = MathEx.Limit((int) Math.Sqrt(a_list.Count), 1, 20);
			for(int j=0; j<a_list.Count; j++)
				{
				Console.Write(a_list[j]);
				Console.Write("  ");
				if (j % cols == cols - 1)
					Console.WriteLine();
				}
			Console.WriteLine();
			}

		static void FindMyBestSecond(string a_first)
			{
			List<string> guesses = new List<string>(s_guesses);
			guesses.Sort(delegate(string a, string b)
				{
				int cmp = Int32Ex.Compare(UShortEx.BitSetCount(GetOverlap(a_first, a)), UShortEx.BitSetCount(GetOverlap(a_first, b)));
				if (cmp == 0)
					cmp = String.Compare(a, b);
				return cmp;
				});

			Dictionary<string, List<string>> buckets = GroupInBuckets(a_first);

			foreach(KeyValuePair<string, List<string>> pair in buckets)
				{
//				Console.WriteLine("Answers in bucket {0}: count = {1}", pair.Key, pair.Value.Count);
//				PrintGuessList(pair.Value);

				AnswerList al = new AnswerList(pair.Value);
				string bestSecond = null;
				double bestProb = 0;

				for(int i=0; i<guesses.Count; i++)
					{
//					if (i % 10000 == 0)
//						Console.WriteLine("Evaluating second {0} of {1}...", i, guesses.Count);

					string second = guesses[i];
					double prob = al.CalculateRandomProbability(new string[] { a_first, second });
					if (prob > bestProb)
						{
						bestProb = prob;
						bestSecond = second;
						}
//					Console.WriteLine("Pair {0} {1} = {2}", a_first, second, prob);
					}

				Console.WriteLine("For bucket {0} with {1} answers, the best pair is {2} {3} with prob of {4:00.00%} of win on n=3", pair.Key, pair.Value.Count, a_first, bestSecond, bestProb);
				}
			}

		static public void CalculateHints(string a_answer, string[] a_guesses, out ulong o_leftRejects, out ulong o_rightRejects, out ulong o_firstBounds, out ulong o_secondBounds)
			{
			int[] answerCounts = new int[15];
			for(int i=0; i<8; i++)
				answerCounts[s_char2index[a_answer[i]]]++;

			int[] mostGuessCounts = new int[15];

			o_leftRejects = 0;
			o_rightRejects = 0;
			foreach(string guess in a_guesses)
				{
				int[] guessCounts = new int[15];
				for(int i=0; i<8; i++)
					{
					guessCounts[s_char2index[guess[i]]]++;

					int charIndex = s_char2index[guess[i]];
					ushort charMap = (ushort) (1 << charIndex);

					if (guess[i] == a_answer[i])
						charMap = (ushort) ~charMap;

					if (i<4)
						o_leftRejects |= (ulong) charMap << (16 * i);
					else
						o_rightRejects |= (ulong) charMap << (16 * (i - 4));
					}

				for(int i=0; i<15; i++)
					{
					if (guessCounts[i] > mostGuessCounts[i])
						mostGuessCounts[i] = guessCounts[i];
					}
				}

			o_firstBounds = 0;
			o_secondBounds = 0;
			for(int i=0; i<15; i++)
				{
				int mg = mostGuessCounts[i];
				int an = answerCounts[i];

				byte cb = 0;
				if (mg > an)
					cb = (byte) (1 << an);
				else
					cb = (byte) ~((1 << mg) - 1);

				if (i<8)
					o_firstBounds |= (ulong) cb << (8 * i);
				else
					o_secondBounds |= (ulong) cb << (8 * (i - 8));
				}
			}

		static private string MakeBucketId_ulongs(string a_answer, string a_guess)
			{
			ulong leftRejects = 0;
			ulong rightRejects = 0;
			ulong firstBounds = 0;
			ulong secondBounds = 0;

			CalculateHints(a_answer, new string[] { a_guess }, out leftRejects, out rightRejects, out firstBounds, out secondBounds);

			leftRejects = leftRejects & 0x001f001f001f001f;
			rightRejects = rightRejects & 0x001f001f001f001f;
			firstBounds = firstBounds & 0x000000ffffffffff;
			secondBounds = 0;

			string bucketId = String.Format("{0:x8}_{1:x8}_{2:x8}_{3:x8}", leftRejects, rightRejects, firstBounds, secondBounds);
			return bucketId;
			}

		static private string MakeBucketId_colours(string a_answer, string a_guess)
			{
			return MakeHintColours(a_answer, a_guess);
			}

		static private string MakeHintColours(string a_answer, string a_guess)
			{
			char[] colours = new char[8];
			char[] answer = a_answer.ToCharArray();

			for(int i=0; i<8; i++)
				{
				if (a_guess[i] == answer[i])
					{
					colours[i] = 'G';
					answer[i] = ' ';
					}
				}
			for(int i=0; i<8; i++)
				{
				if (colours[i] == 'G')
					continue;

				colours[i] = 'B';
				for(int j=0; j<8; j++)
					{
					if (a_guess[i] == answer[j])
						{
						colours[i] = 'P';
						answer[j] = ' ';
						break;
						}
					}
				}

			return new String(colours);
			}

		static private Dictionary<string,List<string>> GroupInBuckets(string a_first)
			{
			Dictionary<string, List<string>> buckets = new Dictionary<string, List<string>>();

			string[] guesses = new string[] { a_first };

			foreach(string answer in s_answers)
				{
				string bucketId = MakeBucketId_colours(answer, a_first);

				List<string> bucket;
				if (buckets.TryGetValue(bucketId, out bucket) == false)
					{
					bucket = new List<string>();
					buckets.Add(bucketId, bucket);
					}
				bucket.Add(answer);
				}

			return buckets;
			}

		static private void SortByOperatorPlacement()
			{
			List<string> guesses = new List<string>(s_guesses);

			Dictionary<string, int> greens = new Dictionary<string, int>();
			Dictionary<string, int> purples = new Dictionary<string, int>();
			foreach(string guess in guesses)
				{
				ushort guessMap = GetCharMap(guess);

				int green = 0;
				int purple = 0;
				foreach(string answer in s_answers)
					{
					for(int i=0; i<8; i++)
						{
						if (answer[i] == guess[i])
							{
							if ((s_char2ushort[guess[i]] & s_opMask) != 0)
								green++;
							}
						else
							{
							if ((s_char2ushort[answer[i]] & s_opMask & guessMap) != 0)
								purple++;
							}
						}
					}
				greens[guess] = green;
				purples[guess] = purple;

				if (greens.Count % 1000 == 0)
					Console.WriteLine("Scoring guesses... {0} of {1}...", greens.Count, guesses.Count);
				}

			guesses.Sort(delegate(string a, string b)
				{
				int cmp = Int32Ex.Compare(UShortEx.BitSetCount((ushort) (GetCharMap(b) & s_opMask)), UShortEx.BitSetCount((ushort) (GetCharMap(a) & s_opMask)));
				if (cmp == 0)
					cmp = Int32Ex.Compare(UShortEx.BitSetCount(GetCharMap(b)), UShortEx.BitSetCount(GetCharMap(a)));
				if (cmp == 0)
					cmp = Int32Ex.Compare(greens[b], greens[a]);
				if (cmp == 0)
					cmp = Int32Ex.Compare(purples[b], purples[a]);
				if (cmp == 0)
					cmp = DoubleEx.Compare(s_singles[b]["Probability"].ToDouble(), s_singles[a]["Probability"].ToDouble());
				if (cmp == 0)
					cmp = String.Compare(a, b);
				return cmp;
				});

			foreach(string guess in guesses)
				Console.WriteLine(guess);

			}

		static private string[] GetHints(string a_answer, string[] a_guesses)
			{
			DebugEx.Assert(s_answers.Contains(a_answer));

			string[] hints = new string[a_guesses.Length];
			for(int i=0; i<a_guesses.Length; i++)
				hints[i] = GetHints(a_answer, a_guesses[i]);
			return hints;
			}

		static private string GetHints(string a_answer, string a_guess)
			{
			DebugEx.Assert(s_guesses.Contains(a_guess));

			char[] hints = new char[8];
			byte[] counts = new byte[128];
			for(int i=0; i<8; i++)
				{
				if (a_answer[i] == a_guess[i])
					hints[i] = 'G';
				else
					{
					hints[i] = '-';
					counts[a_answer[i]]++;
					}
				}
			for(int i=0; i<8; i++)
				{
				if (hints[i] == '-' && counts[a_guess[i]] > 0)
					{
					hints[i] = 'P';
					counts[a_guess[i]]--;
					}
				}

			return new String(hints);
			}

		static private void Test()
			{
			Console.WriteLine("Running tests...");
			Filter.Initialize();

			if (true)
				{
				Filter filt1 = new Filter();
				filt1.AddHint("+0/+01=0", GetHints("1/1+9=10", "+0/+01=0"));
				string key1 = filt1.ToKeyString();

				Filter filt2 = new Filter();
				filt2.AddHint("+0/+01=0", GetHints("2/1+8=10", "+0/+01=0"));
				string key2 = filt1.ToKeyString();

				Console.WriteLine("key1 = {0}", key1);
				Console.WriteLine("key2 = {0}", key2);
				}

			if (true)
				{
				HashSet<string> keys = new HashSet<string>();
				foreach(string guess in s_guesses)
					{
					foreach(string answer in s_answers)
						{
						Filter filt = new Filter();
						filt.AddHint(guess, GetHints(answer, guess));
						keys.Add(filt.ToKeyString());
						}
					Console.WriteLine("{0}: keys={1}", guess, keys.Count);
					}
				}

			if (DebugEx.False)
				{
				foreach(string guess in s_guesses)
					{
					List<int> counts = new List<int>();
					MultiDict<string, string> keys = new MultiDict<string, string>();
					foreach(string answer in s_answers)
						{
						Filter filt = new Filter();
						filt.AddHint(guess, GetHints(answer, guess));

						List<string> matches = filt.FilterAnswers(s_answers);
						counts.Add(matches.Count);
						Console.WriteLine("{0}:{1}:{2} = {3}", guess, answer, filt, matches.Count);
						}
					}
				}

			Console.WriteLine("End of tests.");
			}
		}
	}
*/















	// EOF
