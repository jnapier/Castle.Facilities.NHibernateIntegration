#region License

//  Copyright 2004-2010 Castle Project - http://www.castleproject.org/
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

namespace Castle.Facilities.NHibernateIntegration.Tests.Internals
{
	using System;
	using Common;
	using MicroKernel.Facilities;
	using NHibernate;
	using NUnit.Framework;

	/// <summary>
	/// Tests the default implementation of ISessionStore
	/// </summary>
	[TestFixture]
	public class SessionManagerTestCase : AbstractNHibernateTestCase
	{
		protected override string ConfigurationFile
		{
			get { return "Internals/TwoDatabaseConfiguration.xml"; }
		}

		[Test]
		public void TwoDatabases()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			ISession session1 = manager.OpenSession();
			ISession session2 = manager.OpenSession("db2");

			Assert.IsNotNull(session1);
			Assert.IsNotNull(session2);

			Assert.IsFalse(Object.ReferenceEquals(session1, session2));

			session2.Dispose();
			session1.Dispose();

			Assert.IsTrue(container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void NonInterceptedSession()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			string sessionAlias = "db2";

			ISession session = manager.OpenSession(sessionAlias);
			Order o = new Order();
			o.Value = 9.3f;
			session.SaveOrUpdate(o);
			session.Close();

			session = manager.OpenSession(sessionAlias);
			session.Get(typeof (Order), 1);
			session.Close();

			TestInterceptor interceptor = container.Resolve<TestInterceptor>("nhibernate.session.interceptor.intercepted");
			Assert.IsNotNull(interceptor);
			Assert.IsFalse(interceptor.ConfirmOnSaveCall());
			Assert.IsFalse(interceptor.ConfirmInstantiationCall());
			interceptor.ResetState();
		}

		[Test]
		public void InterceptedSessionByConfiguration()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			string sessionAlias = "intercepted";

			ISession session = manager.OpenSession(sessionAlias);
			Order o = new Order();
			o.Value = 9.3f;
			session.SaveOrUpdate(o);
			session.Close();

			session = manager.OpenSession(sessionAlias);
			session.Get(typeof (Order), 1);
			session.Close();

			TestInterceptor interceptor = container.Resolve<TestInterceptor>("nhibernate.session.interceptor.intercepted");
			Assert.IsNotNull(interceptor);
			Assert.IsTrue(interceptor.ConfirmOnSaveCall());
			Assert.IsTrue(interceptor.ConfirmInstantiationCall());
			interceptor.ResetState();
		}

		[Test]
		public void NonExistentAlias()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			Assert.Throws<FacilityException>(() => manager.OpenSession("something in the way she moves"));
		}

		[Test]
		public void SharedSession()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			ISession session1 = manager.OpenSession();
			ISession session2 = manager.OpenSession();
			ISession session3 = manager.OpenSession();

			Assert.IsNotNull(session1);
			Assert.IsNotNull(session2);
			Assert.IsNotNull(session3);

			Assert.IsTrue(SessionDelegate.AreEqual(session1, session2));
			Assert.IsTrue(SessionDelegate.AreEqual(session1, session3));

			session3.Dispose();
			session2.Dispose();
			session1.Dispose();

			Assert.IsTrue(container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void TwoDatabasesStateless()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			IStatelessSession session1 = manager.OpenStatelessSession();
			IStatelessSession session2 = manager.OpenStatelessSession("db2");

			Assert.IsNotNull(session1);
			Assert.IsNotNull(session2);

			Assert.IsFalse(Object.ReferenceEquals(session1, session2));

			session2.Dispose();
			session1.Dispose();

			Assert.IsTrue(container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}

		[Test]
		public void NonExistentAliasStateless()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			Assert.Throws<FacilityException>(() => manager.OpenStatelessSession("something in the way she moves"));
		}

		[Test]
		public void SharedStatelessSession()
		{
			ISessionManager manager = container.Resolve<ISessionManager>();

			IStatelessSession session1 = manager.OpenStatelessSession();
			IStatelessSession session2 = manager.OpenStatelessSession();
			IStatelessSession session3 = manager.OpenStatelessSession();

			Assert.IsNotNull(session1);
			Assert.IsNotNull(session2);
			Assert.IsNotNull(session3);

			Assert.IsTrue(StatelessSessionDelegate.AreEqual(session1, session2));
			Assert.IsTrue(StatelessSessionDelegate.AreEqual(session1, session3));

			session3.Dispose();
			session2.Dispose();
			session1.Dispose();

			Assert.IsTrue(container.Resolve<ISessionStore>().IsCurrentActivityEmptyFor(Constants.DefaultAlias));
		}
	}
}