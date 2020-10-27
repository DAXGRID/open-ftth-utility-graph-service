using DAX.ObjectVersioning.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Query.InMemory
{
    /// <summary>
    /// Object to hold on to all the versioned object state
    /// </summary>
    public class InMemoryNetworkState : INetworkState
    {
        private InMemoryObjectManager _objectManager = new InMemoryObjectManager();
        
        private bool _loadMode = true;

        private ITransaction? _loadModeTransaction;

        private ITransaction? _cmdTransaction;

        private DateTime __lastEventRecievedTimestamp = DateTime.UtcNow;
        
        private long _numberOfObjectsLoaded = 0;

        public InMemoryObjectManager ObjectManager => _objectManager;
        public DateTime LastEventRecievedTimestamp => __lastEventRecievedTimestamp;
        public long NumberOfObjectsLoaded => _numberOfObjectsLoaded;

        public InMemoryNetworkState()
        {
        }

        public ITransaction GetTransaction()
        {
            if (_loadMode)
                return GetLoadModeTransaction();
            else
                return GetCommandTransaction();
        }

        public void FinishWithTransaction()
        {
            __lastEventRecievedTimestamp = DateTime.UtcNow;
            _numberOfObjectsLoaded++;

            // We're our of load mode, and dealing with last event
            if (!_loadMode && _loadModeTransaction == null)
            {
                // Commit the command transaction
                if (_cmdTransaction != null)
                {
                    _cmdTransaction.Commit();
                    _cmdTransaction = null;
                }
            }
        }

        public IVersionedObject? GetObject(Guid id)
        {
            if (_loadMode && _loadModeTransaction != null)
                return _loadModeTransaction.GetObject(id);
            else if (_cmdTransaction != null)
            {
                var transObj = _cmdTransaction.GetObject(id);

                if (transObj != null)
                    return transObj;
                else
                    return _objectManager.GetObject(id);
            }
            else
                return null;
        }

        private ITransaction GetLoadModeTransaction()
        {
            if (_loadModeTransaction == null)
                _loadModeTransaction = _objectManager.CreateTransaction();

            return _loadModeTransaction;
        }

        private ITransaction GetCommandTransaction()
        {
            if (_cmdTransaction == null)
                _cmdTransaction = _objectManager.CreateTransaction();

            return _cmdTransaction;
        }
    }
}
