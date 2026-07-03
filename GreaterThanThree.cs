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
	internal class GreaterThanThree
		{
		static private Lock s_lock = new Lock();
		static private List<string> s_todo;
		static private List<GreaterThanThree> s_workers = new List<GreaterThanThree>();

		static public void Solve()
			{
			MyTrace.WriteLine("GreaterThanThree working...");

			int threadCount = Environment.ProcessorCount;
			DateTime nextTodo = DateTime.MinValue;

			while(s_workers.Count > 0 || threadCount > 0)
				{
				for(int i=0; i<s_workers.Count; i++)
					{
					if (s_workers[i].m_thread.IsAlive == false)
						{
						s_workers[i].m_thread.Join();
						s_workers.RemoveAt(i);
						i--;
						}
					}

				if (DateTime.UtcNow > nextTodo)
					{
					MakeTodoList();
					nextTodo = DateTime.UtcNow.AddHours(1);
					}

				if (s_workers.Count < threadCount)
					{
					GreaterThanThree worker = new GreaterThanThree();
					worker.m_guess = Equation.FromString(PickNextGuess());
					worker.m_thread = ThreadEx.Fork(worker.WorkerThread);
					s_workers.Add(worker);
					}

				StringBuilder sb = new StringBuilder();
				foreach(GreaterThanThree worker in s_workers)
					{
					sb.AppendFormat("{0} {1,3}% {2,4}, ", worker.m_guess.m_display, worker.m_percentage, worker.m_gt3);
					}
				MyTrace.WriteLine(sb.ToString());

				Thread.Sleep(TimeSpan.FromSeconds(10));
				}
			}

		static public void SolveFirstGuess(string a_equation)
			{
			GreaterThanThree worker = new GreaterThanThree();
			worker.m_guess = Equation.FromString(a_equation);
			worker.m_thread = ThreadEx.Fork(worker.WorkerThread);
			worker.m_thread.Join();
			}

		static private void MakeTodoList()
			{
			MyTrace.WriteLine("Making new todo list...");
			List<string> todo = new List<string>();
			Predictor predictor = new Predictor();
			HashSet<string> done = new HashSet<string>();
			HashSet<string> busy = new HashSet<string>();

			using (s_lock.WriteLock())
				{
				MyTrace.WriteLine("Looking for done guesses...");
				foreach(CsvRow row in CsvFile.ReadRows(Path.Combine(Program.Root, "firsts.csv")))
					{
					done.Add(row["Guess"]);
					string hints = row["Hints"];
					foreach(string tuple in hints.Split('_'))
						{
						int hint = 0;
						int size = 0;
						int gt3 = 0;
						if (tuple.TryMatch("^([0-9]+):([0-9]+):([0-9]+)$", ref hint, ref size, ref gt3))
							predictor.Add(size, gt3);
						}
					}

				MyTrace.WriteLine("Looking at current workers...");
				foreach(GreaterThanThree worker in s_workers)
					busy.Add(worker.m_guess.m_display);
				}

			MyTrace.WriteLine("Estimating guesses...");
			Dictionary<string, double> estimates = new Dictionary<string, double>();
			foreach(string line in File.ReadLines(Path.Combine(Program.Root, "buckets.json")))
				{
				Json json = Json.Parse(line);
				string guess = json["guess"];
				if (done.Contains(guess) || busy.Contains(guess))
					continue;

				double gt3 = 0;
				foreach(int size in json["sizes"].ToIntList())
					gt3 += predictor.Get(size);

				todo.Add(guess);
				estimates.Add(guess, gt3);

				if (todo.Count % 10000 == 0)
					MyTrace.WriteLine("Estimated {0} guesses...", todo.Count);
				}

			MyTrace.WriteLine("Sorting guesses...");
			todo.Sort(delegate(string a, string b) { return DoubleEx.Compare(estimates[a], estimates[b]); });

			using (s_lock.WriteLock())
				{
				s_todo = todo;
				}
			}

		static private string PickNextGuess()
			{
			using (s_lock.WriteLock())
				{
				return s_todo.RemoveFirst();
				}
			}

		public Thread m_thread;
		public Equation m_guess;
		public int m_gt3 = 0;
		public int m_percentage = 0;

		private GreaterThanThree()
			{
			}

		private CsvRow CalculateGt3(Equation a_firstGuess)
			{
//			MyTrace.WriteLine("Processing {0}...", a_firstGuess.m_display);

			CsvRow csv = new CsvRow(new CsvHeader("Guess,GT3,HintCount,Hints"));
			csv["Guess"] = a_firstGuess.m_display;

			Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(Equation.AllAnswers, a_firstGuess);
			csv["HintCount"] = buckets.Count.ToString();

			List<string> hints = new List<string>();
			m_gt3 = 0;
			int total = 0;
			foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
				{
				int hintGt3 = CalculateGt3(a_firstGuess, pair.Key, pair.Value);
				hints.Add(String.Format("{0}:{1}:{2}", pair.Key, pair.Value.Count, hintGt3));
				m_gt3 += hintGt3;
				total += pair.Value.Count;
				m_percentage = total * 100 / Equation.AllAnswers.Count;
				}
			csv["GT3"] = m_gt3.ToString();
			csv["Hints"] = hints.Join('_');
			return csv;
			}

		private int CalculateGt3(Equation a_firstGuess, ushort a_firstHint, List<Equation> a_answerList)
			{
			if (a_firstHint == 0xFF00)
				return 0;
			if (Equation.AreAllTwins(a_answerList))
				return 0;
			if (a_answerList.Count == 2)
				return 0;

			int bestGt3 = int.MaxValue;
			foreach(Equation guess in Equation.AllEquations)
				{
				Dictionary<ushort, List<Equation>> buckets = Hint.SplitAnswersByHint(a_answerList, guess);
				int gt3 = 0;
				foreach(KeyValuePair<ushort, List<Equation>> pair in buckets)
					gt3 += CalculateGt3(a_firstGuess, guess, pair.Key, pair.Value);
				if (gt3 < bestGt3)
					bestGt3 = gt3;
				if (bestGt3 == 0)
					break;
				}

			return bestGt3;
			}

		private int CalculateGt3(Equation a_firstGuess, Equation a_secondGuess, ushort a_secondHint, List<Equation> a_answerList)
			{
//			if (a_secondGuess.m_display == "92*3=276")
//				DebugEx.Nop();

			if (a_secondHint == 0xFF00)
				return 0;
			if (Equation.AreAllTwins(a_answerList))
				return 0;
			if (a_answerList.Count == 2)
				return 1;

			return a_answerList.Count - Equation.LargestTwinSetWithin(a_answerList);
			}

		private void WorkerThread()
			{
//			Equation firstGuess = Equation.FromString("0=000000");
//			Equation firstGuess = Equation.FromString("54-38=16");

			CsvRow row = CalculateGt3(m_guess);
			using (s_lock.WriteLock())
				{
				CsvFile.AppendRows(Path.Combine(Program.Root, "firsts.csv"), new List<CsvRow>() { row });
				}
			}
		}
	}
