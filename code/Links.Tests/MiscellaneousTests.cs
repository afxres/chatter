using System.Linq;
using Xunit;

namespace Mikodev.Links.Tests
{
    public class MiscellaneousTests
    {
        [Fact(DisplayName = "Public Types")]
        public void PublicTypes()
        {
            var types = typeof(LinkFactory).Assembly.GetTypes();
            var publicTypes = types.Where(x => x.IsPublic).ToList();
            Assert.Equal(typeof(LinkFactory), Assert.Single(publicTypes));
        }

        [Fact(DisplayName = "All Not Inheritable")]
        public void AllSealed()
        {
            var types = typeof(LinkFactory).Assembly.GetTypes();
            var ignored = types.Where(x => x.IsValueType || x.IsInterface || x.IsAbstract).ToList();
            var selected = types.Except(ignored).ToList();
            Assert.All(selected, x => Assert.True(x.IsSealed));
        }
    }
}
