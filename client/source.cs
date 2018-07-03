using System;
using System.IO;
using System.Collections.Generic;
namespace Benji.Learner.Client {
  class Program {
    static Random random = new Random();
    static bool E(string[] strings) {
      return strings.Length == 0;
    }
    static string C(string[] strings) {
      return E(strings) ? random.Next(2) == 1 ? (random.Next(2) == 1).ToString() : random.Next(int.MinValue, int.MaxValue).ToString() : strings[random.Next(strings.Length)];
    }
    static double F(string[] strings) {
      double ret;
      foreach (string s in strings)
        if (double.TryParse(s, out ret))
          return ret;
      return (random.Next(2) == 1) ? Math.Log(random.NextDouble()) : -Math.Log(random.NextDouble());
    }
    static double F(string i) {
      double ret;
      return double.TryParse(i, out ret) ? ret : (random.Next(2) == 1) ? Math.Log(random.NextDouble()) : -Math.Log(random.NextDouble());
    }
    static Func<string[], string, string>[] functions = new Func<string[], string, string>[] {
      (s, input) => E(s) ? input : string.Concat(s),
      (s, input) => string.Format(input, s),
      (s, input) => string.Join(input, s),
      (s, input) => string.Compare(input, C(s)).ToString(),
      (s, input) => string.Equals(input, C(s)).ToString(),
      (s, input) => input.Contains(C(s)).ToString(),
      (s, input) => C(s).Contains(input).ToString(),
      (s, input) => input.EndsWith(C(s)).ToString(),
      (s, input) => C(s).EndsWith(input).ToString(),
      (s, input) => input.IndexOf(C(s)).ToString(),
      (s, input) => C(s).IndexOf(input).ToString(),
      (s, input) => input.Replace(C(s), C(s)),
      (s, input) => C(input.Split(C(s).ToCharArray())),
      (s, input) => input.StartsWith(C(s)).ToString(),
      (s, input) => C(s).StartsWith(input).ToString(),
      (s, input) => {
        bool b;
        if (bool.TryParse(C(s), out b))
          return b ? input : C(s);
        int i;
        if (int.TryParse(C(s), out i))
          return new string(input[i], 1);
        return (1 / F(s)).ToString();
      },
      (s, input) => input,
      (s, input) => C(s),
      (s, input) => Math.Cos(F(s)).ToString(),
      (s, input) => Math.Exp(F(s)).ToString(),
      (s, input) => Math.Cos(F(input)).ToString(),
      (s, input) => Math.Exp(F(input)).ToString(),
      (s, input) => (Math.E).ToString(),
      (s, input) => (Math.PI * 2).ToString(),
      (s, input) => (Math.Pow(F(s), F(input))).ToString(),
      (s, input) => (Math.Pow(F(input), F(s))).ToString(),
      (s, input) => (F(s) + F(input)).ToString(),
      (s, input) => (F(s) - F(input)).ToString(),
      (s, input) => (F(s) * F(input)).ToString(),
      (s, input) => (F(s) / F(input)).ToString(),
      (s, input) => input.Length.ToString(),
      (s, input) => s.Length.ToString(),
      (s, input) => C(s).Length.ToString(),
      (s, input) => {
        System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length);
        if (s.Length == 0)
          foreach (char c in input)
            sb.Append((char)(c - 1));
        else
          foreach (char c in input)
            sb.Append((char)(c + 1));
        return sb.ToString();
      },
      (s, input) => new string(input[0], s.Length),
      (s, input) => new string('\0', input.Length),
      (s, input) => new string('\x7f', input.Length),
      (s, input) => new string(input[s.Length], 1),
      (s, input) => {
        int i;
        if (int.TryParse(C(s), out i))
          return new string(input[i], 1);
        return input;
      }
    };
    static void LoadTrainingFile(BinaryReader file, out string[] inputs, out string[][] outputs) {
      int sets = file.ReadInt32();
      inputs = new string[sets];
      outputs = new string[sets][];
      for (int i = 0; i < sets; i++) {
        int choices = file.ReadInt32();
        inputs[i] = file.ReadString();
        outputs[i] = new string[choices];
        for (int j = 0; j < choices; j++) {
          outputs[i][j] = file.ReadString();
        }
      }
    }
    static void MakeTrainingFile(BinaryWriter file, string[] inputs, string[][] outputs) {
      file.Write(inputs.Length);
      for (int i = 0; i < inputs.Length; i++) {
        file.Write(outputs[i].Length);
        file.Write(inputs[i]);
        foreach (string choice in outputs[i])
          file.Write(choice);
      }
    }
    static void MakeSets(out string[] inputs, out string[][] outputs) {
      Console.Write("First input: ");
      List<string> inputs_list = new List<string>();
      inputs_list.Add(Console.ReadLine());
      Console.WriteLine(help_sets);
      List<string> this_output = new List<string>();
      List<string[]> outputs_list = new List<string[]>();
      while (true) {
        Console.Write("> ");
        string input = Console.ReadLine();
        if (input == "new set" || input == "n") {
          Console.Write("Input: ");
          inputs_list.Add(Console.ReadLine());
          Console.Write("Processing...");
          outputs_list.Add(this_output.ToArray());
          this_output.Clear();
          Console.WriteLine();
        } else if (input == "add output" || input == "a") {
          Console.Write("Output: ");
          this_output.Add(Console.ReadLine());
        } else if (input == "help" || input == "h")
          Console.WriteLine(help_sets);
        else if (input == "done" || input == "d") {
          Console.Write("Processing...");
          outputs_list.Add(this_output.ToArray());
          inputs = inputs_list.ToArray();
          outputs = outputs_list.ToArray();
          Console.WriteLine();
          return;
        } else if (input == "clear" || input == "c")
          Console.Clear();
      }
    }
    const string help = "Type \"train\"        or \"t\" to train the networks,\n" +
                        "     \"make trainer\" or \"m\" to make a training info file,\n" +
                        "     \"load trainer\" or \"l\" to use a training info file,\n" +
                        "     \"use\"          or \"u\" to use a network,\n" +
                        "     \"memory usage\" or \"s\" to check the memory usage,\n" +
                        "     \"generation\"   or \"g\" to check the current generation,\n" +
                        "     \"restart\"      or \"R\" to start over,\n" +
                        "     \"load nets\"    or \"L\" to load saved networks,\n" +
                        "     \"save nets\"    or \"S\" to save networks,\n" +
                        "     \"help\"         or \"h\" for this message,\n" +
                        "     \"exit\"         or \"e\" to exit\n" +
                        "  or \"clear\"        or \"c\" to clear the screen.";
    const string help_sets = "Type \"new set\"    or \"n\" to make a new set of inputs and outputs,\n" +
                             "     \"add output\" or \"a\" to add another output to this set,\n" +
                             "     \"help\"       or \"h\" for this message,\n" +
                             "     \"done\"       or \"d\" to use these sets\n" +
                             "  or \"clear\"      or \"c\" to clear the screen.";
    public static void Main() {
      int size;
      do
        Console.Write("Population size: ");
      while (!int.TryParse(Console.ReadLine(), out size));
      Population population = new Population(size, functions);
      Console.WriteLine(help);
      int generations = 0;
      while (true) {
        Console.Write("> ");
        string input = Console.ReadLine();
        if (input == "train" || input == "t") {
          string[] inputs;
          string[][] outputs;
          MakeSets(out inputs, out outputs);
          int times;
          do
            Console.Write("Number of generations: ");
          while (!int.TryParse(Console.ReadLine(), out times));
          int tries;
          do
            Console.Write("Number of tries per network per generation: ");
          while (!int.TryParse(Console.ReadLine(), out tries));
          for (int i = 0; i < times;) {
            Console.Write("{0} / {1}...", ++i, times);
            population.TrainGeneration(inputs, outputs, functions, tries);
            Console.WriteLine();
          }
          generations += times;
        } else if (input == "make trainer" || input == "m") {
          string[] inputs;
          string[][] outputs;
          MakeSets(out inputs, out outputs);
          Console.Write("Training info file: ");
          string filename = Console.ReadLine();
          Console.Write("Saving...");
          using (BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create)))
            MakeTrainingFile(bw, inputs, outputs);
          Console.WriteLine();
        } else if (input == "load trainer" || input == "l") {
          Console.Write("Training info file: ");
          string filename = Console.ReadLine();
          string[] inputs;
          string[][] outputs;
          Console.Write("Loading...");
          using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open)))
            LoadTrainingFile(br, out inputs, out outputs);
          Console.WriteLine();
          int times;
          do
            Console.Write("Number of generations: ");
          while (!int.TryParse(Console.ReadLine(), out times));
          int tries;
          do
            Console.Write("Number of tries per network per generation: ");
          while (!int.TryParse(Console.ReadLine(), out tries));
          for (int i = 0; i < times;) {
            Console.Write("{0} / {0}...", ++i, times);
            population.TrainGeneration(inputs, outputs, functions, tries);
            Console.WriteLine();
          }
          generations += times;
        } else if (input == "use" || input == "u") {
          Console.Write("Text to process: ");
          string text = Console.ReadLine();
          foreach (string output in population.Use(text, Population.ErrorBehavior.use_message_as_output))
            Console.WriteLine(output);
        } else if (input == "help" || input == "h")
          Console.WriteLine(help);
        else if (input == "exit" || input == "e")
          return;
        else if (input == "clear" || input == "c")
          Console.Clear();
        else if (input == "memory usage" || input == "s")
          Console.WriteLine("{0:N0}B", GC.GetTotalMemory(false));
        else if (input == "generation" || input == "g")
          if (generations < 0)
            Console.WriteLine(generations - int.MinValue + " since load");
          else
            Console.WriteLine(generations);
        else if (input == "restart" || input == "R") {
          generations = 0;
          do
            Console.Write("Population size: ");
          while (!int.TryParse(Console.ReadLine(), out size));
          population = new Population(size, functions);
        } else if (input == "load nets" || input == "L") {
          Console.Write("Networks file: ");
          string filename = Console.ReadLine();
          Console.Write("Loading...");
          using (BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open))) {
            ushort version;
            if ((version = br.ReadUInt16()) != 0x8000)
              throw new NotSupportedException(string.Format("Learner 5 Client 1.2 cannot load files with a version other than 0x8000.  This file's version was 0x{0:X2}.", version));
            generations = br.ReadInt32();
            population = new Population(br, functions);
          }
          Console.WriteLine();
        } else if (input == "save nets" || input == "S") {
          Console.Write("Networks file: ");
          string filename = Console.ReadLine();
          Console.Write("Saving...");
          using (BinaryWriter bw = new BinaryWriter(File.Open(filename, FileMode.Create))) {
            bw.Write((ushort)0x8000);
            bw.Write(generations);
            population.Save(bw, functions);
          }
          Console.WriteLine();
        }
      }
    }
  }
}
