#region License
//  Copyright 2004-2012 Castle Project - http://www.castleproject.org/
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//      http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  
#endregion
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
