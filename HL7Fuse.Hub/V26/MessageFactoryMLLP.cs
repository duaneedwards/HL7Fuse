using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HL7Fuse.Hub.V26
{
    /// <summary>
    /// Message factory
    /// </summary>
    public class MessageFactoryMLLP : MessageFactoryBaseMLLP
    {
        #region Public properties
        public override string Name
        {
            get { return "V26.MessageFactoryMLLP"; }
        }
        #endregion
    }
}
