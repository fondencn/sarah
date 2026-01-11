using System;
using System.Collections.Generic;
using System.Text;

namespace Sarah.API.Interfaces
{
    public interface IAssociationGroup
    {
        byte GroupID { get; set; }
        byte[] Nodes { get; set; }
        byte MaxNodesSupported { get; set; }
    }
}
