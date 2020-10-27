using DAX.ObjectVersioning.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenFTTH.UtilityGraphService.Query
{
    public interface INetworkState
    {
        ITransaction GetTransaction();
        void FinishWithTransaction();
        IVersionedObject? GetObject(Guid id);
    }
}
