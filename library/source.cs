using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using Function = System.Func<string[], string, string>;
namespace Benji.Learner {
  /// <summary>
  /// Represents a population of learning networks
  /// </summary>
  public class Population : ICloneable, IEquatable<Population> {
    private static Random prng = new Random();
    /// <summary>
    /// The exception thrown by <see cref="Benji.Learner.Population.Use"/> with <see cref="Benji.Learner.Population.ErrorBehavior.throw_immediately"/>  when an error occurs during processing.
    /// </summary>
    public class UseException : Exception {
      /// <summary>
      /// Initializes a new instance of the <see cref="Benji.Learner.Population.UseException"/> class.
      /// </summary>
      /// <param name="inner">The exception representing the error</param>
      public UseException(Exception inner)
        : base("An error occured inside while processing the string: " + inner.Message, inner) { }
    }
    /// <summary>
    /// A network represented recursively.
    /// </summary>
    protected class Inner : ICloneable, IEquatable<Inner> {
      /// <summary>
      /// Returns a <see cref="Benji.Learner.Population.Inner"/> with nothing deeper than it.<para />
      /// Calling <see cref="Benji.Learner.Population.Inner.Run"/> on this will simply return the argument.
      /// </summary>
      public static Inner MakeBottom() {
        return new Inner(new Function[] { (strings, input) => input }, new List<Inner>());
      }
      /// <summary>
      /// The prng.
      /// </summary>
      private static Random prng = new Random();
      /// <summary>
      /// Initializes a new instance of the <see cref="Benji.Learner.Population.Inner"/> class.
      /// </summary>
      /// <param name="functions">A list of functions to pick one from.</param>
      /// <param name="feeds">The parts of the network below this step.</param>
      /// <exception cref="System.ArgumentNullException">Throw when <paramref name="feeds"/> or <paramref name="functions"/> is null.</exception>
      /// <exception cref="System.ArgumentException">Thrown when <paramref name="functions"/> is an empty array.</exception>
      public Inner(Function[] functions, List<Inner> feeds) {
        if (feeds == null)
          throw new ArgumentNullException("'feeds' must not be null.");
        try {
          function = functions[prng.Next(functions.Length)];
        } catch (IndexOutOfRangeException ex) {
          throw new ArgumentException("'functions' must not be an empty array.", ex);
        } catch (NullReferenceException ex) {
          throw new ArgumentNullException("'functions' must not be null.", ex);
        }
        this.feeds = feeds;
        tree_size = 1;
        foreach (Inner feed in feeds)
          tree_size += feed.tree_size;
      }
      /// <summary>
      /// The function at this step in the network.
      /// </summary>
      protected Function function;
      /// <summary>
      /// The parts of the network below this step. 
      /// </summary>
      protected List<Inner> feeds;
      /// <summary>
      /// The size of the network below and including this step.
      /// </summary>
      protected int tree_size;
      /// <summary>
      /// Run this part of the network on the specfied.
      /// </summary>
      /// <param name="input">The input to use.</param>
      public string Run(string input) {
        string[] arguments = new string[feeds.Count];
        for (int i = 0; i < feeds.Count; i++)
          arguments[i] = feeds[i].Run(input);
        return function(arguments, input);
      }
      private const double done = 0.2, restart = 0.000005, expand = 0.05;
      /// <summary>
      /// Mutate the network below this point using the specified functions when a new function is needed.
      /// </summary>
      /// <param name="functions">A list of functions to pick new functions from.</param>
      public void Mutate(Function[] functions) {
        do {
          if (prng.Next(tree_size) > 1 && feeds.Count != 0) {
            feeds[prng.Next(feeds.Count)].Mutate(functions);
            tree_size = 1;
            foreach (Inner feed in feeds)
              tree_size += feed.tree_size;
          } else if (prng.NextDouble() < restart) {
            function = (strings, input) => input;
            feeds.Clear();
            tree_size = 1;
          } else if (prng.NextDouble() < expand) {
            feeds.Add(MakeBottom());
            tree_size++;
          } else
            function = functions[prng.Next(functions.Length)];
        } while (prng.NextDouble() > done);
      }
      /// <summary>
      /// Clone this instance.
      /// </summary>
      public object Clone() {
        List<Inner> new_feeds = new List<Inner>(feeds.Count);
        foreach (Inner feed in feeds)
          new_feeds.Add((Inner)feed.Clone());
        return new Inner(new Function[] { function }, new_feeds);
      }
      /// <summary>
      /// Determines whether the specified <see cref="Benji.Learner.Population.Inner"/> is equal to the current <see cref="Benji.Learner.Population.Inner"/>.
      /// </summary>
      /// <param name="other">The <see cref="Benji.Learner.Population.Inner"/> to compare with the current <see cref="Benji.Learner.Population.Inner"/>.</param>
      /// <returns><c>true</c> if the specified <see cref="Benji.Learner.Population.Inner"/> is equal to the current
      /// <see cref="Benji.Learner.Population.Inner"/>; otherwise, <c>false</c>.</returns>
      public bool Equals(Inner other) {
        if ((tree_size != other.tree_size) ||
            (function != other.function))
          return false;
        List<Inner> feeds_copy = new List<Inner>(feeds.Count);
        foreach (Inner feed in feeds)
          feeds_copy.Add(feed);
        foreach (Inner feed in other.feeds) {
          for (int i = 0; i < feeds_copy.Count; i++)
            if (feed.Equals(feeds_copy[i])) {
              feeds_copy.RemoveAt(i);
              goto cont;
            }
          return false;
        cont:
          continue;
        }
        return true;
      }
      /// <summary>
      /// Saves a representation of this <see cref="Benji.Learner.Population.Inner"/> that can later be loaded.
      /// </summary>
      /// <param name="bw">The stream to save to.</param>
      /// <param name="functions">A list of the functions used in the network.  The same list must be given in the same order for loading.</param>
      public void Save(System.IO.BinaryWriter bw, Function[] functions) {
        if (functions == null)
          throw new ArgumentNullException("'functions' must not be null.");
        for (int i = 0; i < functions.Length; i++)
          if (functions[i] == function) {
            bw.Write(i);
            goto cont;
          }
        throw new ArgumentException(string.Format("A function {0} is not in the provided list.", function));
      cont:
        bw.Write(feeds.Count);
        foreach (Inner feed in feeds)
          feed.Save(bw, functions);
      }
      /// <summary>
      /// Loads from a file in the format saved by <see cref="Benji.Learner.Population.Inner.Save"/>.
      /// </summary>
      /// <param name="br">The stream to load from.</param>
      /// <param name="functions">A list of the functions used in the network.  The same list should have been given in the same order when saving.</param>
      public Inner(System.IO.BinaryReader br, Function[] functions) {
        function = functions[br.ReadInt32()];
        tree_size = 1;
        int n_feeds = br.ReadInt32();
        feeds = new List<Inner>(n_feeds);
        for (int i = 0; i < n_feeds; i++) {
          Inner feed = new Inner(br, functions);
          tree_size += feed.tree_size;
          feeds.Add(feed);
        }
      }
    }
    /// <summary>
    /// Initializes a new instance of the <see cref="Benji.Learner.Population"/> class.
    /// </summary>
    /// <param name="size">The number of networks in the population at each generation.</param>
    /// <param name="functions">Functions to pick from for networks.</param> 
    /// <remarks>You should train the set before using it.</remarks>
    /// <seealso cref="Benji.Learner.Population.TrainGeneration"/>
    public Population(int size, Function[] functions) {
      if (size < 1)
        throw new ArgumentOutOfRangeException("'size' must be positive.");
      inners = new List<Inner>(this.size = size);
      for (int i = 0; i < size; i++) {
        Inner inner = Inner.MakeBottom();
        inner.Mutate(functions);
        inners.Add(inner);
      }
    }
    /// <summary>
    /// A lock for accessing <see cref="Benji.Learner.Population.inners"/> from the multi-threaded part of <see cref="Benji.Learner.Population.TrainGeneration(string[], string[][], Function[], int)"/>
    /// </summary>
    protected object inners_lock;
    /// <summary>
    /// The networks.
    /// </summary>
    protected List<Inner> inners;
    /// <summary>
    /// The number of networks in the population at each generation.
    /// </summary>
    protected int size;
    /// <summary>
    /// Trains the generation.
    /// </summary>
    /// <param name="inputs">A list of inputs.</param>
    /// <param name="outputs">A list of allowable outputs for each input.</param>
    /// <param name="functions">When a new function is needed at a step in the network, it is picked from this list.</param>
    /// <param name="maxTries">If working networks can't be made in this many tries, add a non-working one in their place.</param> 
    public void TrainGeneration(string[] inputs, string[][] outputs, Function[] functions, int maxTries) {
      if (inputs.Length != outputs.Length)
        throw new ArgumentException("'inputs' and 'outputs' must be the same length.");
      Thread[] threads = new Thread[inners.Count];
      Inner[] tmp = inners.ToArray();
      inners.Clear();
      for (int i = 0; i < size; i++)
        threads[i] = new Thread((ParameterizedThreadStart)((index) => {
          Inner inner = tmp[(int)index];
          int qa_set = prng.Next(inputs.Length);
          string output;
          try {
            output = inner.Run(inputs[qa_set]);
            foreach (string chance in outputs[qa_set])
              if (output == chance) {
                lock (inners_lock) {
                  if (inners.Count >= size)
                    return;
                  inners.Add((Inner)inner.Clone());
                }
                break;
              }
          } catch (Exception) { }
          int tries = 0;
          while (inners.Count < size) {
            inner.Mutate(functions);
            qa_set = prng.Next(inputs.Length);
            try {
              output = inner.Run(inputs[qa_set]);
            } catch (Exception) {
              if (++tries >= maxTries) {
                lock (inners_lock)
                  if (inners.Count < size)
                    inners.Add((Inner)inner.Clone());
                return;
              }
              continue;
            }
            foreach (string chance in outputs[qa_set])
              if (output == chance) {
                lock (inners_lock) {
                  if (inners.Count >= size)
                    return;
                  inners.Add((Inner)inner.Clone());
                }
                break;
              }
            if (++tries >= maxTries) {
              lock (inners_lock)
                if (inners.Count < size)
                  inners.Add((Inner)inner.Clone());
              return;
            }
          }
        }));
      for (int i = 0; i < size; i++)
        threads[i].Start(i);
      for (int i = 0; i < size; i++)
        threads[i].Join();
    }
    /// <summary>
    /// Defines a behavior for when <see cref="Benji.Learner.Population.Use"/> encounters an error during processing.
    /// </summary>
    public enum ErrorBehavior {
      /// <summary>
      /// Skip networks which provide errors.
      /// </summary>
      skip,
      /// <summary>
      /// Throw a <see cref="Benji.Learner.Population.UseException"/> as soon as an error occurs.
      /// </summary>
      throw_immediately,
      /// <summary>
      /// Work the same as <c>skip</c>, but throw a <see cref="System.AggregateException"/> at the end if any errors did occur.
      /// </summary>
      throw_after_all,
      /// <summary>
      /// Return an error message when an error occurs.
      /// </summary>
      use_message_as_output
    }
    /// <summary>
    /// Processes the specified string with each networks.
    /// </summary>
    /// <returns>The result of the processing by each network that didn't throw an error.</returns>
    /// <param name="input">The string to process.</param>
    /// <param name="errorBehavior">A <see cref="Benji.Learner.Population.ErrorBehavior"/> specifying what to do when an error occurs.</param>
    public IEnumerable<string> Use(string input, ErrorBehavior errorBehavior) {
      List<Exception> errors = new List<Exception>();
      string ret = null;
      foreach (Inner inner in inners) {
        try {
          ret = inner.Run(input);
        } catch (Exception ex) {
          switch (errorBehavior) {
          case ErrorBehavior.skip:
            continue;
          case ErrorBehavior.throw_after_all:
            errors.Add(ex);
            continue;
          case ErrorBehavior.throw_immediately:
            throw new UseException(ex);
          case ErrorBehavior.use_message_as_output:
            ret = ex.Message;
            break;
          }
        }
        yield return ret;
      }
      if (errors.Count != 0)
        throw new AggregateException(errors);
    }
    /// <summary>
    /// Clone this instance.
    /// </summary>
    public object Clone() {
      Population ret = new Population(0, new Function[0]);
      ret.inners = new List<Inner>(size);
      foreach (Inner inner in inners)
        ret.inners.Add((Inner)inner.Clone());
      return ret;
    }
    /// <summary>
    /// Determines whether the specified <see cref="Benji.Learner.Population"/> is equal to the current <see cref="Benji.Learner.Population"/>.
    /// </summary>
    /// <param name="other">The <see cref="Benji.Learner.Population"/> to compare with the current <see cref="Benji.Learner.Population"/>.</param>
    /// <returns><c>true</c> if the specified <see cref="Benji.Learner.Population"/> is equal to the current
    /// <see cref="Benji.Learner.Population"/>; otherwise, <c>false</c>.</returns>
    public bool Equals(Population other) {
      if (size != other.size)
        return false;
      List<Inner> inners_copy = new List<Inner>(size);
      foreach (Inner inner in inners)
        inners_copy.Add(inner);
      foreach (Inner inner in other.inners) {
        for (int i = 0; i < inners_copy.Count; i++)
          if (inner.Equals(inners_copy[i])) {
            inners_copy.RemoveAt(i);
            goto cont;
          }
        return false;
      cont:
        continue;
      }
      return true;
    }
    /// <summary>
    /// Saves a representation of this <see cref="Benji.Learner.Population"/> that can later be loaded.
    /// </summary>
    /// <param name="bw">The stream to save to.</param>
    /// <param name="functions">A list of the functions used in the network.  The same list should be given in the same order when loading.</param>
    /// <param name="version">The id of the version to use</param>
    public void Save(System.IO.BinaryWriter bw, Function[] functions = null, ushort version = 0) {
      if (version > 0x7fff)
        throw new ArgumentOutOfRangeException("Version numbers above 0x7fff are reserved for wrappers.");
      if (version != 0)
        throw new NotSupportedException(string.Format("Learner 5.1 cannot save files with a version above 0x0000.  The requested version was 0x{0:X2}.", version));
      bw.Write((ushort)0);
      bw.Write(size);
      foreach (Inner inner in inners)
        inner.Save(bw, functions);
    }
    /// <summary>
    /// Loads from a file in the format saved by <see cref="Benji.Learner.Population.Save"/>.
    /// </summary>
    /// <param name="br">The stream to load from.</param>
    /// <param name="functions">A list of the functions used in the network.  The same list should have been given in the same order when saving.</param>
    public Population(System.IO.BinaryReader br, Function[] functions) {
      ushort version;
      if ((version = br.ReadUInt16()) != 0)
        throw new NotSupportedException(string.Format("Learner 5.1 cannot load files with a version above 0x0000.  This file's version is 0x{0:X2}.", version));
      inners = new List<Inner>(size = br.ReadInt32());
      for (int i = 0; i < size; i++)
        inners.Add(new Inner(br, functions));
    }
  }
}
