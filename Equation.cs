using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Patrick;
using Patrick.Csv2;
using Patrick.Json5;

namespace Nerdle
	{
	//
	// This should replace AnswerEnumerator and GuessEnumerator.
	// This code should enumerator all valid guesses and answers.
	//
	internal class Equation
		{
		public string m_display;
		public int m_equationId;
		public int m_answerId;
		public int m_twinSetId;
		public HashSet<Equation> m_twins;
		public byte[] m_codes = new byte[8];
		public ushort[] m_masks = new ushort[8];
		public ushort m_overallMask;

		static public List<Equation> AllEquations;
		static public List<Equation> AllAnswers;
		static public Dictionary<string, Equation> EquationByDisplay;
		static public int NextEquationId = 0;
		static public int NextAnswerId = 0;
		static public int NextTwinId = 0;
		static public List<Equation>[] RestrictedLists;

		static private char[] s_chars = new char[]				{ '-', '*', '/', '+', '=', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

		public Equation(string a_display)
			{
			m_display = a_display;
			for(int i=0; i<8; i++)
				{
				int index = s_chars.IndexOf(m_display[i]);
				m_codes[i] = (byte) (index + 1);
				m_masks[i] = (ushort) (0x0001 << index);
				m_overallMask |= m_masks[i];
				}
			}

		public override string ToString()
			{
			return m_display;
			}

		public override bool Equals(object obj)
			{
			Equation that = obj as Equation;
			return this.m_equationId == that.m_equationId;
			}

		public override int GetHashCode()
			{
			return m_equationId;
			}

		static public void LoadEquations()
			{
			MyTrace.WriteLine("Loading equations...");

			AllEquations = new List<Equation>();
			EquationByDisplay = new Dictionary<string, Equation>();

			NextEquationId = 0;
			StreamReader sr = new StreamReader(Path.Combine(Program.Root, "guesses.txt"), Encoding.ASCII);
			foreach(string line in sr.EnumerateLines())
				{
				Equation equ = new Equation(line);
				equ.m_equationId = NextEquationId++;
				AllEquations.Add(equ);
				EquationByDisplay[equ.m_display] = equ;
				}
			sr.Close();

			AllAnswers = new List<Equation>();

			NextAnswerId = 0;
			sr = new StreamReader(Path.Combine(Program.Root, "answers.txt"), Encoding.ASCII);
			foreach(string line in sr.EnumerateLines())
				{
				Equation equ = Equation.FromString(line);
				equ.m_answerId = NextAnswerId++;
				AllAnswers.Add(equ);
				}
			sr.Close();

			NextTwinId = 0;
			foreach(Equation equ in AllAnswers)
				{
				if (equ.m_twinSetId == 0)
					{
					equ.m_twins = MakeTwins(equ);
					equ.m_twinSetId = NextTwinId++;
					foreach(Equation twin in equ.m_twins)
						{
						twin.m_twinSetId = equ.m_twinSetId;
						twin.m_twins = equ.m_twins;
						}
					}
				}

//			BuildRestrictedLists();
			}

		static public Equation FromString(string a_string)
			{
			return EquationByDisplay[a_string];
			}

		static public HashSet<Equation> MakeTwins(Equation a_equation)
			{
			string answer = a_equation.m_display;

			HashSet<string> twins = new HashSet<string>();
			string part0 = null;
			string part1 = null;
			string part2 = null;
			string part3 = null;
			if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2))
				{
				twins.Add(String.Format("{0}+{1}={2}", part0, part1, part2));
				twins.Add(String.Format("{1}+{0}={2}", part0, part1, part2));
				}
			else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)\+([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}+{1}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}+{2}+{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}+{0}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}+{2}+{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{0}+{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{1}+{0}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)-([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}+{1}-{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}+{0}-{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}-{2}+{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}-{2}+{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)\*([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}+{1}*{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}+{2}*{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{2}+{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}*{1}+{0}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)/([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}+{1}/{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}/{2}+{0}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)-([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2))
				{
				twins.Add(String.Format("{0}-{1}={2}", part0, part1, part2));
				}
			else if (answer.TryMatch(@"^([0-9]+)-([0-9]+)\+([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}-{1}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}-{1}+{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}+{2}-{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{0}-{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)-([0-9]+)-([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				// TODO: Determine if 10-2-3=5 is a commutation of 10-3-2=5
				twins.Add(String.Format("{0}-{1}-{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}-{2}-{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)-([0-9]+)\*([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}-{1}*{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}-{2}*{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)-([0-9]+)/([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}-{1}/{2}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2))
				{
				twins.Add(String.Format("{0}*{1}={2}", part0, part1, part2));
				twins.Add(String.Format("{1}*{0}={2}", part0, part1, part2));
				}
			else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)\+([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}*{1}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{0}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{0}*{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{1}*{0}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)-([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}*{1}-{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{0}-{2}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)\*([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}*{1}*{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}*{2}*{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{0}*{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{2}*{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}*{0}*{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}*{1}*{0}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)/([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}*{1}/{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}*{0}/{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{1}/{2}*{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}/{2}*{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)/([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2))
				{
				twins.Add(String.Format("{0}/{1}={2}", part0, part1, part2));
				}
			else if (answer.TryMatch(@"^([0-9]+)/([0-9]+)\+([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}/{1}+{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}+{0}/{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)/([0-9]+)-([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}/{1}-{2}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)/([0-9]+)\*([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}/{1}*{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}/{1}*{0}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}*{2}/{1}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{2}*{0}/{1}={3}", part0, part1, part2, part3));
				}
			else if (answer.TryMatch(@"^([0-9]+)/([0-9]+)/([0-9]+)=([0-9]+)$", ref part0, ref part1, ref part2, ref part3))
				{
				twins.Add(String.Format("{0}/{1}/{2}={3}", part0, part1, part2, part3));
				twins.Add(String.Format("{0}/{2}/{1}={3}", part0, part1, part2, part3));
				}
			else
				DebugEx.SelfHalt();

			HashSet<Equation> result = new HashSet<Equation>();
			foreach(string twin in twins)
				{
				DebugEx.Assert(Equation.FromString(twin) != null);
				result.Add(Equation.FromString(twin));
				}
			return result;
			}

		static public Fraction CalculateTheoreticalBestAverage(List<Equation> a_answers)
			{
			Bag<int> idBag = new Bag<int>();
			foreach(Equation answer in a_answers)
				idBag.Add(answer.m_twinSetId);
			int mostCommonTwin = idBag.CountOf(idBag.PickMostPopular());

			Fraction theoreticalBest = new Fraction(a_answers.Count * 2 - mostCommonTwin, a_answers.Count);
			return theoreticalBest;
			}

		static private void BuildRestrictedLists()
			{
			MyTrace.WriteLine("Building restricted lists...");

			RestrictedLists = new List<Equation>[8 * 16];
			for(int i=0; i<8; i++)
				for(int j=1; j<16; j++)
					RestrictedLists[(i*16)+j] = new List<Equation>();

			foreach(Equation equ in AllEquations)
				{
				for(int i=0; i<8; i++)
					RestrictedLists[(i*16)+equ.m_codes[i]].Add(equ);
				}
			}

		static public bool AreAllTwins(List<Equation> a_list)
			{
			int id = a_list[0].m_twinSetId;
			for(int i=1; i<a_list.Count; i++)
				if (a_list[i].m_twinSetId != id)
					return false;
			return true;
			}

		static public int LargestTwinSetWithin(List<Equation> a_list)
			{
			Bag<int> twinIds = new Bag<int>();
			foreach(Equation equ in a_list)
				twinIds.Add(equ.m_twinSetId);
			return twinIds.CountOf(twinIds.PickMostPopular());
			}

		static public int Compare(Equation a, Equation b)
			{
			return String.Compare(a.m_display, b.m_display);
			}




		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
 		//
		// Old static stuff
		//
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		static public List<string> AllAnswerStrings;
		static public List<string> AllGuessStrings;
		static public Dictionary<string, HashSet<string>> Twins;
		static public Dictionary<string, int> TwinSetSize;
		static public Dictionary<string, int> TwinSetId;

		static private int[] s_charToIndex = new int[256];

		static private Dictionary<byte[], double> s_answerListToBestAverageGuessCount = new Dictionary<byte[], double>();
		static private DateTime s_nextSave = DateTime.MinValue;

		static public void Initialize()
			{
			for(int i=0; i<s_chars.Length; i++)
				s_charToIndex[s_chars[i]] = i;

			LoadLists();
			BuildTwins();
			}

		static public int CharToIndex(char a_char)
			{
			return s_charToIndex[a_char];
			}

		static public char IndexToChar(int a_index)
			{
			return s_chars[a_index];
			}

		static public IEnumerable<string> EnumerateAllPermutations()
			{
			long max = (long) Math.Pow(15, 8);
			for(long i = (0) * 1000000; i<max; i++)
				{
				char[] rgc = new char[8];
				long temp = i;
				for(int j=7; j>=0; j--)
					{
					rgc[j] = s_chars[temp % 15];
					temp = temp / 15;
					}
				yield return new string(rgc);
				}
			}

		static private Regex s_guessRegex = new Regex("^([+-]?[0-9]+(?:[-+/*][+-]?[0-9]+){0,2})=([+-]?[0-9]+(?:[-+/*][+-]?[0-9]+){0,2})$");
		static private Regex s_answerRegex = new Regex("^([1-9][0-9]*(?:[-+/*][1-9][0-9]*){0,2})=((?:0|[1-9][0-9]*))$");

		static private bool IsValidGuess(string a_equation)
			{
			char[] rgc = a_equation.ToCharArray();

			if (rgc[0] == '=' || rgc[7] == '=')
				return false;

			int equalCount = 0;
			for(int i=0; i<8; i++)
				{
				if (rgc[i] == '=')
					{
					equalCount++;
					if (equalCount >= 2)
						return false;
					}
				}
			if (equalCount == 0)
				return false;

			DebugEx.Nop();
			Match m = s_guessRegex.Match(a_equation);
			if (m.Success == false)
				return false;
			DebugEx.Assert(m.Groups.Count == 3);

			double lhs = MathEx.Eval(m.Groups[1].Value);
			double rhs = MathEx.Eval(m.Groups[2].Value);

			if (double.IsNaN(lhs) || double.IsNaN(rhs))
				return false;
			if (double.IsInfinity(lhs) || double.IsInfinity(rhs))
				return false;
			if (lhs != rhs)
				return false;

			return true;
			}

		static private bool IsValidAnswer(string a_equation)
			{
			Match m = s_answerRegex.Match(a_equation);
			if (m.Success == false)
				return false;
			DebugEx.Assert(m.Groups.Count == 3);

			double lhs = MathEx.Eval(m.Groups[1].Value);
			int rhs = Int32.Parse(m.Groups[2].Value);

			if (double.IsNaN(lhs))
				return false;
			if (double.IsInfinity(lhs))
				return false;
			if (lhs != rhs)
				return false;

			return true;
			}

		static public void BuildLists()
			{
			List<string> guesses = new List<string>();
			List<string> answers = new List<string>();
			long count = 0;
			foreach(string equ in Equation.EnumerateAllPermutations())
				{
				if (IsValidGuess(equ))
					{
					guesses.Add(equ);
					if (IsValidAnswer(equ))
						answers.Add(equ);
					}

				count++;
				if (count % 1000000 == 0)
					Console.WriteLine("{0} ... Tested {1} million equations...", equ, count / 1000000);
				}

			answers.Sort();
			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "answers.txt"), false, Encoding.ASCII);
			foreach(string equ in answers)
				sw.WriteLine(equ);
			sw.Close();

			guesses.Sort();
			sw = new StreamWriter(Path.Combine(Program.Root, "guesses.txt"), false, Encoding.ASCII);
			foreach(string equ in guesses)
				sw.WriteLine(equ);
			sw.Close();
			}

		static public void LoadLists()
			{
			List<string> answers = new List<string>();
			StreamReader sr = new StreamReader(Path.Combine(Program.Root, "answers.txt"), Encoding.ASCII);
			foreach(string line in sr.EnumerateLines())
				answers.Add(line);
			sr.Close();
			AllAnswerStrings = answers;

			List<string> guesses = new List<string>();
			sr = new StreamReader(Path.Combine(Program.Root, "guesses.txt"), Encoding.ASCII);
			foreach(string line in sr.EnumerateLines())
				guesses.Add(line);
			sr.Close();
			AllGuessStrings = guesses;
			}

		static public void BuildTwins()
			{
			Twins = new Dictionary<string, HashSet<string>>();

			foreach(string answer in AllAnswerStrings)
				{
				HashSet<string> twins = new HashSet<string>();
				string part1 = null;
				string part2 = null;
				string part3 = null;
				string part4 = null;
				if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)\+([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3, ref part4))
					{
					twins.Add(String.Format("{0}+{1}+{2}={3}", part1, part3, part2, part4));
					twins.Add(String.Format("{0}+{1}+{2}={3}", part2, part1, part3, part4));
					twins.Add(String.Format("{0}+{1}+{2}={3}", part2, part3, part1, part4));
					twins.Add(String.Format("{0}+{1}+{2}={3}", part3, part1, part2, part4));
					twins.Add(String.Format("{0}+{1}+{2}={3}", part3, part2, part1, part4));
					}
				else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)\*([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3, ref part4))
					{
					twins.Add(String.Format("{0}*{1}*{2}={3}", part1, part3, part2, part4));
					twins.Add(String.Format("{0}*{1}*{2}={3}", part2, part1, part3, part4));
					twins.Add(String.Format("{0}*{1}*{2}={3}", part2, part3, part1, part4));
					twins.Add(String.Format("{0}*{1}*{2}={3}", part3, part1, part2, part4));
					twins.Add(String.Format("{0}*{1}*{2}={3}", part3, part2, part1, part4));
					}
				else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)\*([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3, ref part4))
					{
					twins.Add(String.Format("{0}+{1}*{2}={3}", part1, part3, part2, part4));
					twins.Add(String.Format("{0}*{1}+{2}={3}", part2, part3, part1, part4));
					twins.Add(String.Format("{0}*{1}+{2}={3}", part3, part2, part1, part4));
					}
				else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)\+([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3, ref part4))
					{
					twins.Add(String.Format("{0}*{1}+{2}={3}", part2, part1, part3, part4));
					twins.Add(String.Format("{0}+{1}*{2}={3}", part3, part1, part2, part4));
					twins.Add(String.Format("{0}+{1}*{2}={3}", part3, part2, part1, part4));
					}
				else if (answer.TryMatch(@"^([0-9]+)\+([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3))
					{
					twins.Add(String.Format("{0}+{1}={2}", part2, part1, part3));
					}
				else if (answer.TryMatch(@"^([0-9]+)\*([0-9]+)=([0-9]+)$", ref part1, ref part2, ref part3))
					{
					twins.Add(String.Format("{0}*{1}={2}", part2, part1, part3));
					}

				twins.Remove(answer);

				foreach(string twin in twins)
					DebugEx.Assert(AllAnswerStrings.Contains(twin));

				if (twins.Count > 0)
					Twins.Add(answer, twins);
				}

			TwinSetSize = new Dictionary<string, int>();
			foreach(string answer in AllAnswerStrings)
				{
				if (Twins.ContainsKey(answer))
					TwinSetSize.Add(answer, Twins[answer].Count + 1);
				else
					TwinSetSize.Add(answer, 1);
				}

			TwinSetId = new Dictionary<string, int>();
			int nextId = 1000000;
			foreach(string answer in AllAnswerStrings)
				{
				if (TwinSetId.ContainsKey(answer))
					continue;

				HashSet<string> twins;
				if (Twins.TryGetValue(answer, out twins))
					{
					TwinSetId[answer] = nextId;
					foreach(string twin in twins)
						TwinSetId[twin] = nextId;
					}
				else
					{
					TwinSetId[answer] = nextId;
					}
				nextId++;
				}
			}

		static public void WriteHint(string a_guess, ushort a_hint)
			{
			for(int i=0; i<8; i++)
				{
				if ((a_hint & (1 << 8+(7-i))) != 0)
					{
					Console.BackgroundColor = ConsoleColor.DarkGreen;
					}
				else if ((a_hint & (1 << 7-i)) != 0)
					{
					Console.BackgroundColor = ConsoleColor.DarkMagenta;
					}
				Console.Write(a_guess[i]);
				Console.BackgroundColor = ConsoleColor.Black;
				}
			}

		static public string HintToString(ushort a_hint)
			{
			StringBuilder sb = new StringBuilder();
			for(int i=0; i<8; i++)
				{
				if ((a_hint & (1 << 8+(7-i))) != 0)
					sb.Append('G');
				else if ((a_hint & (1 << 7-i)) != 0)
					sb.Append('p');
				else
					sb.Append('.');
				}
			return sb.ToString();
			}

		static private void SortAllGuessStrings()
			{
			StreamReader sr = new StreamReader(Path.Combine(Program.Root, "buckets.json"));
			Dictionary<string, int> bucketCounts = new Dictionary<string, int>();
			foreach(string line in sr.EnumerateLines())
				{
				Json json = Json.Parse(line);
				bucketCounts[json["guess"]] = json["count"].ToInt();
				}
			sr.Close();

			List<string> sorted = new List<string>(bucketCounts.Keys);
			sorted.Sort(delegate(string a, string b) { return Int32Ex.Compare(bucketCounts[b], bucketCounts[a]); });
			AllGuessStrings = sorted;

			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "guesses.txt"), false, Encoding.ASCII);
			foreach(string equ in sorted)
				sw.WriteLine(equ);
			sw.Close();
			}

		static private void ConvertBucketsToCsv()
			{
			StreamReader sr = new StreamReader(Path.Combine(Program.Root, "buckets.json"));
			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "buckets.csv"));
			sw.WriteLine("Guess,Count,Average,StdDev");
			foreach(string line in sr.EnumerateLines())
				{
				Json json = Json.Parse(line);
				sw.WriteLine("{0},{1},{2},{3}", json["guess"], json["count"], json["average"], json["stddev"]);
				}
			sw.Close();
			sr.Close();
			}

		static private byte[] HashAnswerList(List<string> a_answers)
			{
			MD5Hash hash = new MD5Hash();
			foreach(string answer in a_answers)
				hash.Add(answer);
			hash.Finish();
			return hash.GetBytes();
			}

		static public byte[] HashAnswerList(List<Equation> a_answers)
			{
			MD5Hash hash = new MD5Hash();
			foreach(Equation answer in a_answers)
				hash.Add(answer.m_codes);
			hash.Finish();
			return hash.GetBytes();
			}

		static public bool AreTwins(string a_equation1, string a_equation2)
			{
			HashSet<string> set;
			if (Twins.TryGetValue(a_equation1, out set))
				return set.Contains(a_equation2);
			return false;
			}

		static public bool AreAllTwins_old(List<string> a_answers)
			{
			HashSet<string> twins;
			if (Twins.TryGetValue(a_answers[0], out twins))
				{
				for(int i=1; i<a_answers.Count; i++)
					{
					if (twins.Contains(a_answers[i]) == false)
						return false;
					}
				return true;
				}
			return false;
			}

		static private void LoadCheatSheet()
			{
			foreach(CsvRow row in CsvFile.ReadRows(Path.Combine(Program.Root, "cheat.csv")))
				s_answerListToBestAverageGuessCount[row["hash"].FromHex()] = row["value"].ToDouble();
			}

		static private void SaveCheatSheet()
			{
			List<CsvRow> rows = new List<CsvRow>();
			foreach(KeyValuePair<byte[],double> pair in s_answerListToBestAverageGuessCount)
				{
				CsvRow row = new CsvRow();
				row["hash"] = pair.Key.ToHex();
				row["value"] = pair.Value.ToString();
				rows.Add(row);
				}
			CsvFile.WriteRows(Path.Combine(Program.Root, "cheat.csv"), rows);
			s_nextSave = DateTime.UtcNow.AddSeconds(60);
			}

		static public void WriteEquationList(List<string> a_equations)
			{
			Console.WriteLine("Equation list, count = {0}:", a_equations.Count);
			foreach(string equation in a_equations)
				Console.WriteLine("    {0}", equation);
			}

		static public Fraction CalculateTheoreticalBestAverage(List<string> a_answers)
			{
			Bag<int> idBag = new Bag<int>();
			foreach(string answer in a_answers)
				idBag.Add(TwinSetId[answer]);
			int mostCommonTwin = idBag.CountOf(idBag.PickMostPopular());

			Fraction theoreticalBest = new Fraction(a_answers.Count * 2 - mostCommonTwin, a_answers.Count);
			return theoreticalBest;
			}
		}
	}
