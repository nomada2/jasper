﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Baseline;
using Jasper.Configuration;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.Model;
using Jasper.Messaging.Runtime;
using Jasper.Messaging.Transports.Sending;
using Jasper.Messaging.WorkerQueues;
using Jasper.Util;

namespace Jasper.Messaging.Transports
{
    public abstract class TransportBase : ITransport
    {
        private readonly IList<IListener> _listeners = new List<IListener>();

        private ListeningStatus _status = ListeningStatus.Accepting;

        public TransportBase(string protocol, ITransportLogger logger,
            JasperOptions options)
        {
            this.logger = logger;
            JasperOptions = options;
            Protocol = protocol;
        }

        public IWorkerQueue WorkerQueue { get; private set; }

        public JasperOptions JasperOptions { get; }

        protected ITransportLogger logger { get; }

        public string Protocol { get; }
        public Uri ReplyUri { get; protected set; }



        public ISendingAgent BuildSendingAgent(Uri uri, IMessagingRoot root, CancellationToken cancellation)
        {
            try
            {
                var batchedSender = createSender(uri, cancellation);


                var agent = uri.IsDurable()
                    ? root.BuildDurableSendingAgent(uri, batchedSender)
                    : new LightweightSendingAgent(uri, batchedSender, logger, JasperOptions);

                agent.DefaultReplyUri = ReplyUri;
                agent.Start();

                return agent;
            }
            catch (Exception e)
            {
                throw new TransportEndpointException(uri, "Could not build sending agent. See inner exception.", e);
            }
        }

        public void StartListening(IMessagingRoot root)
        {
            var options = root.Options;

            if (options.StateFor(Protocol) == TransportState.Disabled) return;

            WorkerQueue = root.Workers;

            var incoming = options.Listeners.Where(x => x.Scheme == Protocol).ToArray();

            incoming = validateAndChooseReplyChannel(incoming);

            foreach (var listenerSettings in incoming)
            {
                try
                {
                    var agent = buildListeningAgent(listenerSettings, options, root.Handlers);
                    agent.Status = _status;

                    var worker = listenerSettings.IsDurable
                        ? (IWorkerQueue) new DurableWorkerQueue(listenerSettings, root.Pipeline, options, root.Persistence, logger)
                        : new LightweightWorkerQueue(listenerSettings, logger, root.Pipeline, options);

                    var persistence = listenerSettings.IsDurable
                        ? root.Persistence
                        : new NulloEnvelopePersistence(worker);

                    var listener = new Listener(agent, worker, logger, options,  persistence);

                    _listeners.Add(listener);

                    listener.Start();
                }
                catch (Exception e)
                {
                    throw new TransportEndpointException(listenerSettings.Uri, "Could not build listening agent. See inner exception.", e);
                }
            }
        }

        public IEnumerable<IListener> Listeners => _listeners;

        public void Describe(TextWriter writer)
        {
            writer.WriteLine($"Transport '{Protocol}' is active");
            foreach (var listener in _listeners) writer.WriteLine($"Listening at {listener.Address}");
        }

        public ListeningStatus ListeningStatus
        {
            get => _status;
            set
            {
                _status = value;
                foreach (var listener in _listeners) listener.Status = value;
            }
        }

        public void Dispose()
        {
            foreach (var listener in _listeners) listener.SafeDispose();

            _listeners.Clear();
        }

        protected abstract ISender createSender(Uri uri, CancellationToken cancellation);

        protected abstract ListenerSettings[] validateAndChooseReplyChannel(ListenerSettings[] incoming);
        protected abstract IListeningAgent buildListeningAgent(ListenerSettings listenerSettings, JasperOptions options,
            HandlerGraph handlers);
    }
}
