namespace Castle.Facilities.NHibernateIntegration.Tests.Configuration
{
	using AutoTx;
	using Core.Configuration;
	using MicroKernel.Facilities;
	using NUnit.Framework;
	using Windsor;

	[TestFixture]
    public class FacilityFluentConfigTestCase
    {
        [Test]
        public void Should_be_able_to_revolve_ISessionManager_when_fluently_configured()
        {
            var container = new WindsorContainer();

        	container.AddFacility<AutoTxFacility>();
            container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder<TestConfigurationBuilder>());

            var sessionManager = container.Resolve<ISessionManager>();
            sessionManager.OpenSession();
            Assert.AreEqual(typeof(TestConfigurationBuilder), container.Resolve<IConfigurationBuilder>().GetType());
        }

        [Test]
        public void Should_override_DefaultConfigurationBuilder()
        {
            var container = new WindsorContainer();

			container.AddFacility<AutoTxFacility>();
            container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder<DummyConfigurationBuilder>());

            Assert.AreEqual(typeof(DummyConfigurationBuilder), container.Resolve<IConfigurationBuilder>().GetType());
        }

        [Test, ExpectedException(typeof(FacilityException))]
        public void Should_not_accept_non_implementors_of_IConfigurationBuilder_for_override()
        {
            var container = new WindsorContainer();

            container.AddFacility<NHibernateFacility>(f => f.ConfigurationBuilder(GetType()));
        }
    }


    class DummyConfigurationBuilder : IConfigurationBuilder
    {
        public NHibernate.Cfg.Configuration GetConfiguration(IConfiguration config)
        {
            return new NHibernate.Cfg.Configuration();
        }
    }
}
