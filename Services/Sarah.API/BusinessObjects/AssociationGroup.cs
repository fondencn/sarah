using Sarah.API.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.BusinessObjects
{
    public class AssociationGroup : IAssociationGroup
    {
        public byte GroupID { get; set; }
        public byte[] Nodes { get; set; } = new byte[0];
        public byte MaxNodesSupported { get; set; }
    }
}
