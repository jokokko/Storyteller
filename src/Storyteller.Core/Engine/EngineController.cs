﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FubuCore;
using Storyteller.Core.Engine.UserInterface;
using Storyteller.Core.Messages;
using Storyteller.Core.Model;
using Storyteller.Core.Model.Persistence;
using Storyteller.Core.Remotes.Messaging;

namespace Storyteller.Core.Engine
{
    public class EngineController : IResultObserver,
        IListener<RunSpec>,
        IListener<RunSpecs>,
        IListener<HierarchyLoaded>,
        IListener<CancelSpec>,
        IListener<CancelAllSpecs>
    {
        private readonly ISpecificationEngine _engine;
        private readonly IUserInterfaceObserver _observer;
        private Task<Suite> _hierarchy;

        private readonly IList<SpecExecutionRequest> _outstanding = new List<SpecExecutionRequest>();


        public EngineController(ISpecificationEngine engine, IUserInterfaceObserver observer)
        {
            _engine = engine;
            _observer = observer;
            _hierarchy = Task.Factory.StartNew(() => HierarchyLoader.ReadHierarchy());
        }

        public IEnumerable<SpecExecutionRequest> OutstandingRequests()
        {
            return _outstanding.ToArray();
        } 

        public void Receive(RunSpec message)
        {
            RunSpec(message.id, message.spec);
        }

        public virtual void RunSpec(string id, Specification specification = null)
        {
            if (OutstandingRequests().Any(x => x.Node.id == id)) return;

            var spec = findSpec(id);
            if (spec == null) return;

            var request = new SpecExecutionRequest(spec, this, specification);
            _outstanding.Add(request);

            _observer.SpecQueued(spec);

            _engine.Enqueue(request);
        }

        private SpecNode findSpec(string id)
        {
            _hierarchy.Wait(2.Seconds());

            return _hierarchy.Result.GetAllSpecs().FirstOrDefault(x => x.id == id);
        }

        void IResultObserver.Handle<T>(T message)
        {
            _observer.SendToClient(message);
        }

        public void SpecExecutionFinished(SpecNode node, SpecResults results)
        {
            var outstanding = _outstanding.FirstOrDefault(x => x.Node == node);
            if (outstanding == null) return;

            _outstanding.Remove(outstanding);

            _observer.SendToClient(new SpecExecutionCompleted(node.id, results));
        }

        public void Receive(HierarchyLoaded message)
        {
            _hierarchy = Task.FromResult(message.hierarchy);
        }

        public void Receive(CancelSpec message)
        {
            CancelSpec(message.id);
            
        }

        public virtual void CancelSpec(string id)
        {
            _outstanding.Where(x => x.Node.id == id).Each(x =>
            {
                x.Cancel();
            });
            _outstanding.RemoveAll(x => x.Node.id == id);

            _observer.SendToClient(new SpecCanceled(id));
        }

        public void Receive(CancelAllSpecs message)
        {
            var outstanding = OutstandingRequests();
            outstanding.Each(x =>
            {
                x.Cancel();
                _observer.SendToClient(new SpecCanceled(x.Node.id));
            });

            _outstanding.Clear();
        }

        public void Receive(RunSpecs message)
        {
            message.list.Each(x => RunSpec(x));
        }
    }
}