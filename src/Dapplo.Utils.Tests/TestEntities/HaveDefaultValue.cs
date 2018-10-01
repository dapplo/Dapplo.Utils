using System.ComponentModel;

namespace Dapplo.Utils.Tests.TestEntities
{
    public class HaveDefaultValue : IHaveDefaultValue

    {
        #region Implementation of IHaveDefaultValue

        [DefaultValue("ClassValue")]
        public string MyValue { get; }

        #endregion
    }
}
