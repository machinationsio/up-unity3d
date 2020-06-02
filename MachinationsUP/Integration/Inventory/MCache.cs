using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MachinationsUP.Integration.Elements;

namespace MachinationsUP.Integration.Inventory
{

    [DataContract(Name = "MachinationsCache", Namespace = "http://www.machinations.io")]
    public class MCache
    {

        [DataMember()]
        public List<DiagramMapping> DiagramMappings = new List<DiagramMapping>();

    }
}
