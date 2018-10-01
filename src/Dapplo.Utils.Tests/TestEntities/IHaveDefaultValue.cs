using System.ComponentModel;

namespace Dapplo.Utils.Tests.TestEntities
{
    public interface IHaveDefaultValue
    {
        [DefaultValue("CorrectValue")]
        string MyValue { get; }
    }
}
