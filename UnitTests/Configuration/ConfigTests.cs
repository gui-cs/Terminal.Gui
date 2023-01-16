using Xunit;
using Terminal.Gui.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terminal.Gui.ConfigurationTests
{
	public class ConfigTests {
		[Fact]
		public void GetHardCodedDefaults_ShouldBeImplementedByDerivedClass ()
		{
			var config = new TestConfig ();
			Assert.Throws<NotImplementedException> (() => config.GetHardCodedDefaults ());
		}

		[Fact]
		public void Apply_ShouldBeImplementedByDerivedClass ()
		{
			var config = new TestConfig ();
			Assert.Throws<NotImplementedException> (() => config.Apply ());
		}

		[Fact]
		public void CopyUpdatedProperitesFrom_ShouldBeImplementedByDerivedClass ()
		{
			var config = new TestConfig ();
			var changedConfig = new TestConfig ();
			Assert.Throws<NotImplementedException> (() => config.CopyUpdatedProperitesFrom (changedConfig));
		}

		private class TestConfig : Config<TestConfig> {
			public override void GetHardCodedDefaults ()
			{
				throw new NotImplementedException ();
			}

			public override void Apply ()
			{
				throw new NotImplementedException ();
			}

			public override void CopyUpdatedProperitesFrom (TestConfig changedConfig)
			{
				throw new NotImplementedException ();
			}
		}
	}

}