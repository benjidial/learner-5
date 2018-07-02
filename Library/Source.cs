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
      private const double done = 0.2, restart = 0.002, expand = 0.05;
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
      this.size = size;
      inners = new ConcurrentBag<Inner>();
      for (int i = 0; i < size; i++) {
        Inner inner = Inner.MakeBottom();
        inner.Mutate(functions);
        inners.Add(inner);
      }
    }
    /// <summary>
    /// The networks.
    /// </summary>
    protected ConcurrentBag<Inner> inners;
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
    /// <param name="maxTries">If a working network can't be made in this many tries, add a non-working one in its place.</param> 
    public void TrainGeneration(string[] inputs, string[][] outputs, Function[] functions, int maxTries) {
      if (inputs.Length != outputs.Length)
        throw new ArgumentException("'inputs' and 'outputs' must be the same length.");
      Thread[] threads = new Thread[inners.Count];
      Inner[] tmp = inners.ToArray();
      for (int i = 0; i < size; i++)
        threads[i] = new Thread((ParameterizedThreadStart)((index) => {
          Inner inner = tmp[(int)index];
          int tries = 0;
          while (inners.Count < size) {
            inner.Mutate(functions);
            int qa_set = prng.Next(inputs.Length);
            string output;
            try {
              output = inner.Run(inputs[qa_set]);
            } catch (Exception) {
              if (++tries >= maxTries)
                inners.Add((Inner)inner.Clone());
              continue;
            }
            foreach (string chance in outputs[qa_set])
              if (output == chance) {
                inners.Add((Inner)inner.Clone());
                break;
              }
            if (++tries >= maxTries)
              inners.Add((Inner)inner.Clone());
          }
        }));
      Inner dummy;
      while (inners.TryTake(out dummy))
        ;
      for (int i = 0; i < size; i++)
        threads[i].Start(i);
      for (int i = 0; i < size; i++)
        threads[i].Join();
      while (inners.Count > size)
        inners.TryTake(out dummy);
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
      ret.inners = new ConcurrentBag<Inner>();
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
  }
}
