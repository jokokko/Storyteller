﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FubuCore;
using StoryTeller.Engine;
using StoryTeller.Model;
using StoryTeller.Results;

namespace StoryTeller
{
    public class SpecContext : ISpecContext
    {
        public readonly IList<IResultMessage> Results = new List<IResultMessage>();
        private readonly Specification _specification;
        private readonly IResultObserver _resultObserver;
        private IServiceLocator _services;
        private readonly State _state = new State();
        private bool _hasCatastrophicException;
        private bool _hasCriticalException;
        private readonly Timings _timings;

        public SpecContext(Specification specification, Timings timings, IResultObserver observer, StopConditions stopConditions, IServiceLocator services)
        {
            Counts = new Counts();
            _specification = specification;
            _resultObserver = observer;
            StopConditions = stopConditions;
            _services = services;

            _timings = timings ?? new Timings();

            Reporting = new Reporter();
        }

        public StopConditions StopConditions { get; private set; }
        public Reporter Reporting { get; private set; }

        public SpecResults FinalizeResults(int attempts)
        {
            var performance = _timings.Finish().ToArray();

            return new SpecResults
            {
                Counts = Counts,
                Results = Results.ToArray(),
                Performance = performance,
                Duration = _timings.Duration,
                Reporting = Reporting.GenerateReports(),
                Attempts = attempts,
                HadCriticalException = HadCriticalException
            };

            
        }

        public void Dispose()
        {
            Reporting.Dispose();
            _state.Dispose();
            _services = null;
        }

        public Timings Timings
        {
            get { return _timings; }
        }

        public Specification Specification
        {
            get { return _specification; }
        }

        public Counts Counts { get; private set; }

        public bool CanContinue()
        {
            if (_hasCriticalException || _hasCatastrophicException)
                return false;

            return StopConditions.CanContinue(Counts);
        }

        public bool HadCatastrophicException
        {
            get { return _hasCatastrophicException; }
        }

        public bool HadCriticalException
        {
            get { return _hasCriticalException; }
        }

        public bool Wait(Func<bool> condition, TimeSpan timeout, int millisecondPolling = 500)
        {
            if (condition()) return true;

            var clock = new Stopwatch();
            clock.Start();

            try
            {
                while (clock.Elapsed < timeout)
                {
                    Thread.Yield();
                    Thread.Sleep(millisecondPolling);

                    if (condition()) return true;
                }

                return false;
            }
            finally
            {
                clock.Stop();
            }
        }

        public T Service<T>()
        {
            return _services.GetInstance<T>();
        }

        public State State
        {
            get { return _state; }
        }


        public void LogResult<T>(T result) where T : IResultMessage
        {
            if (result.id.IsEmpty())
                throw new ArgumentOutOfRangeException("result", "The id of the result cannot be empty");

            result.spec = Specification.id;

            _resultObserver.Handle(result);
            result.Tabulate(Counts);
            Results.Add(result);
        }

        public void LogException(string id, Exception ex, object position = null)
        {
            if (ex is StorytellerCriticalException)
            {
                _hasCriticalException = true;

                // TODO -- hokey. Watch if this becomes a pattern
                if (ex.InnerException is MissingFixtureException)
                {
                    LogResult(new StepResult(id, ResultStatus.invalid)
                    {
                        error = ex.InnerException.Message,
                        position = position
                    });
                    return;
                }
            }

            if (ex is StorytellerCatastrophicException) _hasCatastrophicException = true;

            LogResult(new StepResult(id, ResultStatus.error) {error = ex.ToString(), position = position});
        }


        public static SpecContext Basic(IServiceLocator services = null)
        {
            return new SpecContext(new Specification(), null, new NulloResultObserver(), new StopConditions(), services ?? new InMemoryServiceLocator());
        }

        public static SpecContext ForTesting()
        {
            SpecContext context = Basic();

            return context;
        }
    }
}