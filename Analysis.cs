using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Patrick;

namespace Nerdle
	{
	static internal class Analysis
		{
		static public void SymbolDistribution()
			{
			int[,] counts = new int[8,15];
			foreach(string answer in Equation.AllAnswerStrings)
				{
				for(int i=0; i<8; i++)
					{
					int s = Equation.CharToIndex(answer[i]);
					counts[i,s]++;
					}
				}

			StreamWriter sw = new StreamWriter(Path.Combine(Program.Root, "distrib.csv"));
			for(int i=0; i<8; i++)
				sw.Write(",P{0}", i);
			sw.WriteLine();
			for(int s=0; s<15; s++)
				{
				sw.Write(Equation.IndexToChar(s));
				for(int i=0; i<8; i++)
					sw.Write(",{0}", counts[i,s]);
				sw.WriteLine();
				}
			sw.Close();
			}

		static public void AnswerRegexMatches()
			{
			Regex re = new Regex("^[1-9][0-9]*(?:[-+/*][1-9][0-9]*){0,2}=(?:0|[1-9][0-9]*)$");
			int count = 0;
			foreach(string answer in Equation.EnumerateAllPermutations())
				{
				if (re.Match(answer).Success)
					count++;
				}
			Console.WriteLine("Permutations that match answer regex = {0}", count);
			}
		}
	}
