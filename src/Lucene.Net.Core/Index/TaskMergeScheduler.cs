﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.Linq;
    using System.Threading.Tasks;
    using Util;

    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    using Directory = Lucene.Net.Store.Directory;

    /// <summary>
    ///  A <seealso cref="MergeScheduler"/> that runs each merge using
    ///  Tasks on the default TaskScheduler.
    /// 
    ///  <p>If more than <seealso cref="#GetMaxMergeCount"/> merges are
    ///  requested then this class will forcefully throttle the
    ///  incoming threads by pausing until one more more merges
    ///  complete.</p>
    /// </summary>
    public class TaskMergeScheduler : MergeScheduler, IConcurrentMergeScheduler
    {
        public const string COMPONENT_NAME = "CMS";

        private readonly TaskScheduler _taskScheduler = TaskScheduler.Default;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly ManualResetEventSlim _manualResetEvent = new ManualResetEventSlim();
        /// <summary>
        /// List of currently active <seealso cref="MergeThread"/>s.</summary>
        private readonly IList<MergeThread> _mergeThreads = new List<MergeThread>();

        /// <summary>
        /// How many <seealso cref="MergeThread"/>s have kicked off (this is use
        ///  to name them).
        /// </summary>
        private int _mergeThreadCount;

        /// <summary>
        /// <seealso cref="Directory"/> that holds the index. </summary>
        private Directory _directory;

        /// <summary>
        /// <seealso cref="IndexWriter"/> that owns this instance.
        /// </summary>
        private IndexWriter _writer;

        /// <summary>
        /// Sole constructor, with all settings set to default
        ///  values.
        /// </summary>
        public TaskMergeScheduler() : base()
        {
            MaxThreadCount = _taskScheduler.MaximumConcurrencyLevel;
            MaxMergeCount = _taskScheduler.MaximumConcurrencyLevel;
        }

        /// <summary>
        /// Sets the maximum number of merge threads and simultaneous merges allowed.
        /// </summary>
        /// <param name="maxMergeCount"> the max # simultaneous merges that are allowed.
        ///       If a merge is necessary yet we already have this many
        ///       threads running, the incoming thread (that is calling
        ///       add/updateDocument) will block until a merge thread
        ///       has completed.  Note that we will only run the
        ///       smallest <code>maxThreadCount</code> merges at a time. </param>
        /// <param name="maxThreadCount"> the max # simultaneous merge threads that should
        ///       be running at once.  this must be &lt;= <code>maxMergeCount</code> </param>
        public void SetMaxMergesAndThreads(int maxMergeCount, int maxThreadCount)
        {
            // This is handled by TaskScheduler.Default.MaximumConcurrencyLevel
        }

        /// <summary>
        /// Max number of merge threads allowed to be running at
        /// once.  When there are more merges then this, we
        /// forcefully pause the larger ones, letting the smaller
        /// ones run, up until maxMergeCount merges at which point
        /// we forcefully pause incoming threads (that presumably
        /// are the ones causing so much merging).
        /// </summary>
        /// <seealso cref= #setMaxMergesAndThreads(int, int)  </seealso>
        public int MaxThreadCount { get; private set; }

        /// <summary>
        /// Max number of merges we accept before forcefully
        /// throttling the incoming threads
        /// </summary>
        public int MaxMergeCount { get; private set; }

        /// <summary>
        /// Return the priority that merge threads run at. This is always the same.
        /// </summary>
        public int MergeThreadPriority
        {
            get
            {
#if NETCORE
                return 2;
#else
                return (int)ThreadPriority.Normal;
#endif 
            }
            set
            {
            }
        }

        /// <summary>
        /// Called whenever the running merges have changed, to pause & unpause
        /// threads. this method sorts the merge threads by their merge size in
        /// descending order and then pauses/unpauses threads from first to last --
        /// that way, smaller merges are guaranteed to run before larger ones.
        /// </summary>
        private void UpdateMergeThreads()
        {
            foreach (var merge in _mergeThreads.ToArray())
            {
                // Prune any dead threads
                if (!merge.IsAlive)
                {
                    _mergeThreads.Remove(merge);
                    merge.Dispose();
                }
            }
        }

        /// <summary>
        /// Returns true if verbosing is enabled. this method is usually used in
        /// conjunction with <seealso cref="#message(String)"/>, like that:
        ///
        /// <pre class="prettyprint">
        /// if (verbose()) {
        ///   message(&quot;your message&quot;);
        /// }
        /// </pre>
        /// </summary>
        protected internal bool Verbose()
        {
            return _writer != null && _writer.infoStream.IsEnabled(COMPONENT_NAME);
        }

        /// <summary>
        /// Outputs the given message - this method assumes <seealso cref="#verbose()"/> was
        /// called and returned true.
        /// </summary>
        protected internal virtual void Message(string message)
        {
            _writer.infoStream.Message(COMPONENT_NAME, message);
        }

        public override void Dispose()
        {
            Sync();
            _manualResetEvent.Dispose();
        }

        /// <summary>
        /// Wait for any running merge threads to finish. 
        /// This call is not interruptible as used by <seealso cref="#Dispose()"/>.
        /// </summary>
        public virtual void Sync()
        {
            foreach (var merge in _mergeThreads.ToArray())
            {
                if (!merge.IsAlive)
                {
                    continue;
                }

                try
                {
                    merge.Wait();
                }
                catch (OperationCanceledException)
                {
                    // expected when we cancel.
                }
                catch (AggregateException ae)
                {
                    ae.Handle(x => x is OperationCanceledException);

                    foreach (var exception in ae.InnerExceptions)
                    {
                        HandleMergeException(ae);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of merge threads that are alive. Note that this number
        /// is &lt;= <seealso cref="#mergeThreads"/> size.
        /// </summary>
        private int MergeThreadCount()
        {
            return _mergeThreads.Count(x => x.IsAlive && x.CurrentMerge != null);
        }

        public override void Merge(IndexWriter writer, MergeTrigger trigger, bool newMergesFound)
        {
            using (_lock.Write())
            {
                _writer = writer;
                _directory = writer.Directory;

                if (Verbose())
                {
                    Message("now merge");
                    Message("  index: " + writer.SegString());
                }

                // First, quickly run through the newly proposed merges
                // and add any orthogonal merges (ie a merge not
                // involving segments already pending to be merged) to
                // the queue.  If we are way behind on merging, many of
                // these newly proposed merges will likely already be
                // registered.

                // Iterate, pulling from the IndexWriter's queue of
                // pending merges, until it's empty:
                while (true)
                {
                    long startStallTime = 0;
                    while (writer.HasPendingMerges() && MergeThreadCount() >= MaxMergeCount)
                    {
                        // this means merging has fallen too far behind: we
                        // have already created maxMergeCount threads, and
                        // now there's at least one more merge pending.
                        // Note that only maxThreadCount of
                        // those created merge threads will actually be
                        // running; the rest will be paused (see
                        // updateMergeThreads).  We stall this producer
                        // thread to prevent creation of new segments,
                        // until merging has caught up:
                        startStallTime = Environment.TickCount;
                        if (Verbose())
                        {
                            Message("    too many merges; stalling...");
                        }

                        _manualResetEvent.Reset();
                        _manualResetEvent.Wait();
                    }

                    if (Verbose())
                    {
                        if (startStallTime != 0)
                        {
                            Message("  stalled for " + (Environment.TickCount - startStallTime) + " msec");
                        }
                    }

                    MergePolicy.OneMerge merge = writer.NextMerge;
                    if (merge == null)
                    {
                        if (Verbose())
                        {
                            Message("  no more merges pending; now return");
                        }
                        return;
                    }

                    bool success = false;
                    try
                    {
                        if (Verbose())
                        {
                            Message("  consider merge " + writer.SegString(merge.Segments));
                        }

                        // OK to spawn a new merge thread to handle this
                        // merge:
                        var merger = CreateTask(writer, merge);

                        merger.MergeThreadCompleted += OnMergeThreadCompleted;

                        _mergeThreads.Add(merger);

                        if (Verbose())
                        {
                            Message("    launch new thread [" + merger.Name + "]");
                        }

                        merger.Start(_taskScheduler);

                        // Must call this after starting the thread else
                        // the new thread is removed from mergeThreads
                        // (since it's not alive yet):
                        UpdateMergeThreads();

                        success = true;
                    }
                    finally
                    {
                        if (!success)
                        {
                            writer.MergeFinish(merge);
                        }
                    }
                }
            }
        }

        private void OnMergeThreadCompleted(object sender, EventArgs e)
        {
            var mergeThread = sender as MergeThread;

            if (mergeThread == null)
            {
                return;
            }

            mergeThread.MergeThreadCompleted -= OnMergeThreadCompleted;

            using (_lock.Write())
            {
                UpdateMergeThreads();
            }
        }

        /// <summary>
        /// Create and return a new MergeThread </summary>
        private MergeThread CreateTask(IndexWriter writer, MergePolicy.OneMerge merge)
        {
            var count = Interlocked.Increment(ref _mergeThreadCount);
            var name = string.Format("Lucene Merge Task #{0}", count);

            return new MergeThread(name, writer, merge, writer.infoStream, _manualResetEvent, HandleMergeException);
        }

        /// <summary>
        /// Called when an exception is hit in a background merge
        ///  thread
        /// </summary>
        protected internal virtual void HandleMergeException(Exception exc)
        {
            // suppressExceptions is normally only set during testing
            if (SuppressExceptions)
            {
                return;
            }
#if !NETCORE
            try
            {
#endif
                // When an exception is hit during merge, IndexWriter
                // removes any partial files and then allows another
                // merge to run.  If whatever caused the error is not
                // transient then the exception will keep happening,
                // so, we sleep here to avoid saturating CPU in such
                // cases:
                Thread.Sleep(250);
#if !NETCORE
            }
            catch (ThreadInterruptedException ie)
            {
                throw new ThreadInterruptedException("Thread Interrupted Exception", ie);
            }
#endif
            throw new MergePolicy.MergeException(exc, _directory);
        }

        private bool SuppressExceptions;

        /// <summary>
        /// Used for testing </summary>
        public virtual void SetSuppressExceptions()
        {
            SuppressExceptions = true;
        }

        /// <summary>
        /// Used for testing </summary>
        public virtual void ClearSuppressExceptions()
        {
            SuppressExceptions = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(this.GetType().Name + ": ");
            sb.AppendFormat("maxThreadCount={0}, ", MaxThreadCount);
            sb.AppendFormat("maxMergeCount={0}", MaxMergeCount);
            return sb.ToString();
        }

        public override IMergeScheduler Clone()
        {
            TaskMergeScheduler clone = (TaskMergeScheduler)base.Clone();
            clone._writer = null;
            clone._directory = null;
            clone._mergeThreads.Clear();
            return clone;
        }

        /// <summary>
        /// Runs a merge thread, which may run one or more merges
        ///  in sequence.
        /// </summary>
        internal class MergeThread : IDisposable
        {
            public event EventHandler MergeThreadCompleted;

            private readonly CancellationTokenSource _cancellationTokenSource;
            private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
            private readonly ManualResetEventSlim _resetEvent;
            private readonly Action<Exception> _exceptionHandler;
            private readonly InfoStream _logger;
            private readonly IndexWriter _writer;
            private readonly MergePolicy.OneMerge _startingMerge;

            private Task _task;
            private MergePolicy.OneMerge _runningMerge;
            private volatile bool _isDisposed = false;
            private volatile bool _isDone;

            /// <summary>
            /// Sole constructor. </summary>
            public MergeThread(string name, IndexWriter writer, MergePolicy.OneMerge startMerge, InfoStream logger,
                ManualResetEventSlim resetEvent, Action<Exception> exceptionHandler)
            {
                Name = name;
                _cancellationTokenSource = new CancellationTokenSource();
                _writer = writer;
                _startingMerge = startMerge;
                _logger = logger;
                _resetEvent = resetEvent;
                _exceptionHandler = exceptionHandler;
            }

            private bool IsLoggingEnabled
            {
                get
                {
                    return _logger != null && _logger.IsEnabled(COMPONENT_NAME);
                }
            }

            public string Name { get; private set; }

            public Task Instance
            {
                get
                {
                    using (_lock.Read())
                    {
                        return _task;
                    }
                }
            }

            /// <summary>
            /// Record the currently running merge. </summary>
            public virtual MergePolicy.OneMerge RunningMerge
            {
                set
                {
                    Interlocked.Exchange(ref _runningMerge, value);
                }
                get
                {
                    using (_lock.Read())
                    {
                        return _runningMerge;
                    }
                }
            }

            /// <summary>
            /// Return the current merge, or null if this {@code
            ///  MergeThread} is done.
            /// </summary>
            public virtual MergePolicy.OneMerge CurrentMerge
            {
                get
                {
                    using (_lock.Read())
                    {
                        if (_isDone)
                        {
                            return null;
                        }

                        return _runningMerge ?? _startingMerge;
                    }
                }
            }

            public bool IsAlive
            {
                get
                {
                    if (_isDisposed || _isDone)
                    {
                        return false;
                    }

                    using (_lock.Read())
                    {
                        return _task != null
                            && (_task.Status != TaskStatus.Canceled
                            || _task.Status != TaskStatus.Faulted
                            || _task.Status != TaskStatus.RanToCompletion);
                    }
                }
            }

            public void Start(TaskScheduler taskScheduler)
            {
                using (_lock.Write())
                {
                    if (_task == null && !_cancellationTokenSource.IsCancellationRequested)
                    {
                        _task = Task.Factory.StartNew(() => Run(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.None, taskScheduler);
                    }
                }
            }

            public void Wait()
            {
                if (!IsAlive)
                {
                    return;
                }

                _task.Wait(_cancellationTokenSource.Token);
            }

            public void Cancel()
            {
                if (!IsAlive)
                {
                    return;
                }

                using (_lock.Write())
                {
                    if (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        _cancellationTokenSource.Cancel();
                    }
                }
            }

            private void Run(CancellationToken cancellationToken)
            {
                // First time through the while loop we do the merge
                // that we were started with:
                MergePolicy.OneMerge merge = _startingMerge;

                try
                {
                    if (IsLoggingEnabled)
                    {
                        _logger.Message(COMPONENT_NAME, "  merge thread: start");
                    }

                    while (true && !cancellationToken.IsCancellationRequested)
                    {
                        RunningMerge = merge;
                        _writer.Merge(merge);

                        // Subsequent times through the loop we do any new
                        // merge that writer says is necessary:
                        merge = _writer.NextMerge;

                        // Notify here in case any threads were stalled;
                        // they will notice that the pending merge has
                        // been pulled and possibly resume:
                        _resetEvent.Set();

                        if (merge != null)
                        {
                            if (IsLoggingEnabled)
                            {
                                _logger.Message(COMPONENT_NAME, "  merge thread: do another merge " + _writer.SegString(merge.Segments));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (IsLoggingEnabled)
                    {
                        _logger.Message(COMPONENT_NAME, "  merge thread: done");
                    }
                }
                catch (Exception exc)
                {
                    // Ignore the exception if it was due to abort:
                    if (!(exc is MergePolicy.MergeAbortedException))
                    {
                        //System.out.println(Thread.currentThread().getName() + ": CMS: exc");
                        //exc.printStackTrace(System.out)
                        _exceptionHandler(exc);
                    }
                }
                finally
                {
                    _isDone = true;

                    if (MergeThreadCompleted != null)
                    {
                        MergeThreadCompleted(this, EventArgs.Empty);
                    }
                }
            }

            public void Dispose()
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                _lock.Dispose();
                _cancellationTokenSource.Dispose();
            }

            public override string ToString()
            {
                return _task == null
                    ? string.Format("Task[{0}], Task has not been started yet.", Name)
                    : string.Format("Task[{0}], Id[{1}], Status[{2}]", Name, _task.Id, _task.Status);
            }

            public override bool Equals(object obj)
            {
                var compared = obj as MergeThread;

                if (compared == null
                    || (Instance == null && compared.Instance != null)
                    || (Instance != null && compared.Instance == null))
                {
                    return false;
                }

                return Instance.Id == compared.Instance.Id;
            }

            public override int GetHashCode()
            {
                return Instance == null
                    ? base.GetHashCode()
                    : Instance.GetHashCode();
            }
        }
    }
}