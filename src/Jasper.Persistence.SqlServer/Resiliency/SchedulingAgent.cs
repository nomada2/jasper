﻿using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using Jasper.Messaging;
using Jasper.Messaging.Durability;
using Jasper.Messaging.Logging;
using Jasper.Messaging.WorkerQueues;

namespace Jasper.Persistence.SqlServer.Resiliency
{
    public class SchedulingAgent : SchedulingAgentBase<IMessagingAction>
    {
        private readonly SqlServerSettings _mssqlSettings;
        private SqlConnection _connection;


        public SchedulingAgent(ISubscriberGraph subscribers, IWorkerQueue workers, SqlServerSettings mssqlSettings,
            JasperOptions settings, ITransportLogger logger, IRetries retries)
            : base(settings, logger,
                new RunScheduledJobs(workers, mssqlSettings, logger, retries, settings),
                new RecoverIncomingMessages(workers, settings, mssqlSettings, logger),
                new RecoverOutgoingMessages(subscribers, settings, mssqlSettings, logger),
                new ReassignFromDormantNodes(mssqlSettings, settings)
            )
        {
            _mssqlSettings = mssqlSettings;
        }


        protected override void disposeConnection()
        {
            _connection?.Dispose();
        }

        protected override async Task processAction(IMessagingAction action)
        {
            await tryRestartConnection();

            if (_connection == null) return;

            try
            {
                try
                {
                    Debug.WriteLine($"Running {action}");
                    await action.Execute(_connection, this);
                }
                catch (Exception e)
                {
                    logger.LogException(e, message: "Running " + action);
                }
            }
            catch (Exception e)
            {
                logger.LogException(e, message: "Error trying to run " + action);
                _connection?.Dispose();
                _connection = null;
            }

            await tryRestartConnection();
        }

        private async Task tryRestartConnection()
        {
            if (_connection?.State == ConnectionState.Open) return;

            if (_connection != null)
                try
                {
                    _connection.Close();
                    _connection.Dispose();
                    _connection = null;
                }
                catch (Exception e)
                {
                    logger.LogException(e);
                }

            try
            {
                _connection = new SqlConnection(_mssqlSettings.ConnectionString);

                await _connection.OpenAsync(settings.Cancellation);

                await retrieveLockForThisNode();
            }
            catch (Exception e)
            {
                logger.LogException(e);

                _connection?.Dispose();
                _connection = null;
            }
        }


        protected override async Task openConnectionAndAttainNodeLock()
        {
            _connection = new SqlConnection(_mssqlSettings.ConnectionString);

            await _connection.OpenAsync(settings.Cancellation);

            await retrieveLockForThisNode();
        }


        protected override async Task releaseNodeLockAndClose()
        {
            await _connection.ReleaseGlobalLock(settings.UniqueNodeId);

            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }

        private Task retrieveLockForThisNode()
        {
            return _connection.GetGlobalLock(settings.UniqueNodeId);
        }
    }
}
