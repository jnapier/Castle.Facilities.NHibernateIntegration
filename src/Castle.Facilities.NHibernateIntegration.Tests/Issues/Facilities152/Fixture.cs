namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities152
{
	using Core.Resource;
	using NUnit.Framework;
	using SessionStores;
	using Windsor;
	using Windsor.Configuration.Interpreters;

	[TestFixture]
    public class Fixture
    {
        [Test]
        public void Should_Read_IsWeb_Configuration_From_Xml_Registration()
        {
            var file1 = "Castle.Facilities.NHibernateIntegration.Tests/Issues.Facilities152.facilityweb.xml";
            var file2 = "Castle.Facilities.NHibernateIntegration.Tests/Issues.Facilities152.facilitynonweb.xml";

            var containerWhenIsWebTrue = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file1)));

            var containerWhenIsWebFalse = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file2)));

            var sessionStoreWhenIsWebTrue = containerWhenIsWebTrue.Resolve<ISessionStore>();

            var sessionStoreWhenIsWebFalse = containerWhenIsWebFalse.Resolve<ISessionStore>();

            Assert.IsInstanceOf(typeof(WebSessionStore), sessionStoreWhenIsWebTrue);
            Assert.IsInstanceOf(typeof(CallContextSessionStore), sessionStoreWhenIsWebFalse);
        }
    }
}