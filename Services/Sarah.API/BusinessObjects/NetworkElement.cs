using Sarah.API.Interfaces;
using System;
using System.Threading.Tasks;
using Sarah.API.Interfaces.Services;

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
        /// <param name="network">Das Netzwerk, in dem das Gerät registriert ist</param>
        /// <returns>Task</returns>
        public virtual Task InitializeAsync(IDeviceService network) => Task.CompletedTask;

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

        /// <summary>
        /// Liefert die Assoziationsgruppen des Geräts
        /// </summary>
        public virtual Task<IAssociationGroup[]> GetAssociationGroups(IDeviceService network)
        {
            return (network.GetAssociationGroups(this.NodeID)) ;

        }

        /// <summary>
        /// Setzt die Assoziationsgruppe
        /// </summary>
        public virtual Task SetAssociationGroup(IDeviceService network, byte groupId, byte[] nodeIds)
        {
            return (network.SetAssociationGroup(this.NodeID, groupId, nodeIds));

        }
    }
}
