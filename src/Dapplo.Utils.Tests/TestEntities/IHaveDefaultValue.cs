using System.ComponentModel;

namespace Dapplo.Utils.Tests.TestEntities
{
    [Description("Testing 3 2 1")]
    public interface IHaveDefaultValue
    {
        [DefaultValue("InterfaceValue")]
        string MyValue { get; }
    }
}
