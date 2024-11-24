using Sarah.API.Interfaces;
using System;
using System.Threading.Tasks;

namespace Sarah.API.BusinessObjects
{
    /// <summary>
    /// Basisklasse für alle Geräteelemente aus dem Netzwerk
    /// </summary>
    public abstract class NetworkElement : INetworkElement
    {
        /// <summary>
        /// Name
        /// </summary>
        public virtual string Name => String.Format("{0} ({1:000})", ClassDescription, NodeID);

        /// <summary>
        /// Zwave Node-ID
        /// </summary>
        public byte NodeID { get; }

        /// <summary>
        /// Beschreibung der Geräteart
        /// </summary>
        public virtual string ClassDescription { get => this.GetType().Name; }

        /// <summary>
        /// Zustandsbeschreibung des Geräts
        /// </summary>
        public virtual string StateInfo { get; } = "Zustand unbekannt";

        /// <summary>
        /// Gibt an, ob dieses Gerät gerade irgendwie aktiv ist
        /// </summary>
        public virtual bool? IsActive { get; } = null;

        /// <summary>
        /// Kann überschrieben werden, um Task-basierten initialisierungscode auszuführen
        /// </summary>
        /// <returns></returns>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="nodeid">ZWave Node ID des Geräts</param>
        public NetworkElement(byte nodeid) { this.NodeID = nodeid; }

        /// <summary>
        /// Übergibt ein beliebiges Netzwerkereignis an den Event Aggregator
        /// </summary>
        /// <param name="e"></param>
        protected void ReportEvent(NetworkEvent e)
        {
            NetworkEventAggregator.Instance.Report(e);
        }

        // public virtual Task<IAssociationGroup[]> GetAssociationGroups()
        // {
        //     return (InteLukNetworkFactory.InteLukNetwork.GetAssociationGroups(this.NodeID)) ;

        // }

        // public virtual Task SetAssociationGroup(byte groupId, byte[] nodeIds)
        // {
        //     return (InteLukNetworkFactory.InteLukNetwork.SetAssociationGroup(this.NodeID, groupId, nodeIds));

        // }
    }
}
