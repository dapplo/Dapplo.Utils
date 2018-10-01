using System.ComponentModel;

namespace Dapplo.Utils.Tests.TestEntities
{
    public interface IHaveDefaultValue
    {
        [DefaultValue("InterfaceValue")]
        string MyValue { get; }
    }
}
