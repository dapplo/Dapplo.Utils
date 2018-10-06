using System.ComponentModel;

namespace Dapplo.Utils.Tests.TestEntities
{
    [Description("Testing 1 2 3")]
    public class HaveDefaultValue : IHaveDefaultValue

    {
        #region Implementation of IHaveDefaultValue

        [DefaultValue("ClassValue")]
        public string MyValue { get; }

        #endregion
    }
}
