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
namespace Castle.Facilities.NHibernateIntegration.Tests.Transactions
{
	using System;
	using MicroKernel.Registration;
	using NHibernate;
	using NUnit.Framework;

	[TestFixture]
	public class TransactionsTestCase : AbstractNHibernateTestCase
	{
		protected override string ConfigurationFile
		{
			get { return "Transactions/TwoDatabaseConfiguration.xml"; }
		}

		protected override void ConfigureContainer()
		{
			container.Register(Component.For<RootService>().Named("root"));
			container.Register(Component.For<FirstDao>().Named("myfirstdao"));
			container.Register(Component.For<SecondDao>().Named("myseconddao"));
		}

		[Test]
		public void TestTransactionUsingDetachedCriteria()
		{
			var service = container.Resolve<RootService>();

			string blogName = "Delicious Food!";

			var blogA = service.CreateBlogStatelessUsingDetachedCriteria(blogName);
			Assert.IsNotNull(blogA);

			var blogB = service.FindBlogUsingDetachedCriteria(blogName);
			Assert.IsNotNull(blogB);

			Assert.AreEqual(blogA.Name, blogB.Name);
		}

		[Test]
		public void TestTransactionStatelessUsingDetachedCriteria()
		{
			var service = container.Resolve<RootService>();

			string blogName = "Delicious Food!";

			var blogA = service.CreateBlogStatelessUsingDetachedCriteria(blogName);
			Assert.IsNotNull(blogA);

			var blogB = service.FindBlogStatelessUsingDetachedCriteria(blogName);
			Assert.IsNotNull(blogB);

			Assert.AreEqual(blogA.Name, blogB.Name);
		}

		[Test]
		public void TestTransaction()
		{
			var service = container.Resolve<RootService>();
			var dao = container.Resolve<FirstDao>("myfirstdao");

			Blog blog = dao.Create("Blog1");

			try
			{
				service.DoBlogRefOperation(blog);

				// Expects a constraint exception on Commit
				Assert.Fail("Must fail");
			}
			catch (Exception)
			{
				// transaction exception expected
			}
		}

		[Test]
		public void TransactionNotHijackingTheSession()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (ISession session = sessionManager.OpenSession())
			{
				transaction = session.Transaction;

				Assert.IsFalse(transaction.IsActive);

				var service = container.Resolve<FirstDao>("myfirstdao");

				// This call is transactional
				Blog blog = service.Create();

				Assert.IsFalse(transaction.IsActive);

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAll(typeof (Blog));
				Assert.AreEqual(1, blogs.Length);
			}

			Assert.IsFalse(transaction.WasCommitted);
		}

		[Test]
		public void SessionBeingSharedByMultipleTransactionsInSequence()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (ISession session = sessionManager.OpenSession())
			{
				transaction = session.Transaction;
				Assert.IsFalse(transaction.IsActive);

				var service = container.Resolve<FirstDao>("myfirstdao");

				// This call is transactional
				service.Create();

				// This call is transactional
				service.Create("ps2's blogs");

				// This call is transactional
				service.Create("game cube's blogs");

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAll(typeof (Blog));
				Assert.AreEqual(3, blogs.Length);
			}

			Assert.IsFalse(transaction.WasCommitted);
		}

		[Test]
		public void NonTransactionalRoot()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (ISession session = sessionManager.OpenSession())
			{
				transaction = session.Transaction;

				Assert.IsFalse(transaction.IsActive);

				var first = container.Resolve<FirstDao>("myfirstdao");
				var second = container.Resolve<SecondDao>("myseconddao");

				// This call is transactional
				Blog blog = first.Create();

				// TODO: Assert transaction was committed
				// Assert.IsTrue(session.Transaction.WasCommitted);

				try
				{
					second.CreateWithException2(blog);
				}
				catch (Exception)
				{
					// Expected
				}

				// TODO: Assert transaction was rolledback
				// Assert.IsTrue(session.Transaction.WasRolledBack);

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAll(typeof (Blog));
				Assert.AreEqual(1, blogs.Length);
				Array blogitems = rootService.FindAll(typeof (BlogItem));
				Assert.IsEmpty(blogitems);
			}
		}

		[Test]
		public void SimpleAndSucessfulSituationUsingRootTransactionBoundary()
		{
			var service = container.Resolve<RootService>();

			service.SuccessFullCall();

			Array blogs = service.FindAll(typeof (Blog));
			Array blogitems = service.FindAll(typeof (BlogItem));

			Assert.IsNotNull(blogs);
			Assert.IsNotNull(blogitems);
			Assert.AreEqual(1, blogs.Length);
			Assert.AreEqual(1, blogitems.Length);
		}

		[Test]
		public void CallWithException()
		{
			var service = container.Resolve<RootService>();

			try
			{
				service.CallWithException();
			}
			catch (NotSupportedException)
			{
			}

			// Ensure rollback happened

			Array blogs = service.FindAll(typeof (Blog));
			Array blogitems = service.FindAll(typeof (BlogItem));

			Assert.IsEmpty(blogs);
			Assert.IsEmpty(blogitems);
		}

		[Test]
		public void CallWithException2()
		{
			var service = container.Resolve<RootService>();

			try
			{
				service.CallWithException2();
			}
			catch (NotSupportedException)
			{
			}

			// Ensure rollback happened

			Array blogs = service.FindAll(typeof (Blog));
			Array blogitems = service.FindAll(typeof (BlogItem));

			Assert.IsEmpty(blogs);
			Assert.IsEmpty(blogitems);
		}

		[Test]
		public void TestTransactionStateless()
		{
			var service = container.Resolve<RootService>();
			var dao = container.Resolve<FirstDao>("myfirstdao");

			Blog blog = dao.CreateStateless("Blog1");

			try
			{
				service.DoBlogRefOperationStateless(blog);

				// Expects a constraint exception on Commit
				Assert.Fail("Must fail");
			}
			catch (Exception)
			{
				// transaction exception expected
			}
		}

		[Test]
		public void TransactionNotHijackingTheStatelessSession()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (IStatelessSession session = sessionManager.OpenStatelessSession())
			{
				transaction = session.Transaction;

				Assert.IsFalse(transaction.IsActive);

				var service = container.Resolve<FirstDao>("myfirstdao");

				// This call is transactional
				Blog blog = service.CreateStateless();

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAllStateless(typeof(Blog));
				Assert.AreEqual(1, blogs.Length);
			}

			Assert.IsFalse(transaction.WasCommitted);
		}

		[Test]
		public void SessionBeingSharedByMultipleTransactionsInSequenceStateless()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (IStatelessSession session = sessionManager.OpenStatelessSession())
			{
				transaction = session.Transaction;
				Assert.IsFalse(transaction.IsActive);

				var service = container.Resolve<FirstDao>("myfirstdao");

				// This call is transactional
				service.CreateStateless();

				// This call is transactional
				service.CreateStateless("ps2's blogs");

				// This call is transactional
				service.CreateStateless("game cube's blogs");

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAllStateless(typeof(Blog));
				Assert.AreEqual(3, blogs.Length);
			}

			Assert.IsFalse(transaction.WasCommitted);
		}

		[Test]
		public void NonTransactionalRootStateless()
		{
			var sessionManager = container.Resolve<ISessionManager>();

			ITransaction transaction;

			using (IStatelessSession session = sessionManager.OpenStatelessSession())
			{
				transaction = session.Transaction;

				Assert.IsFalse(transaction.IsActive);

				var first = container.Resolve<FirstDao>("myfirstdao");
				var second = container.Resolve<SecondDao>("myseconddao");

				// This call is transactional
				Blog blog = first.CreateStateless();

				// TODO: Assert transaction was committed
				// Assert.IsTrue(session.Transaction.WasCommitted);

				try
				{
					second.CreateWithExceptionStateless2(blog);
				}
				catch (Exception)
				{
					// Expected
				}

				// TODO: Assert transaction was rolledback
				// Assert.IsTrue(session.Transaction.WasRolledBack);

				var rootService = container.Resolve<RootService>();

				Array blogs = rootService.FindAllStateless(typeof(Blog));
				Assert.AreEqual(1, blogs.Length);
				Array blogitems = rootService.FindAllStateless(typeof(BlogItem));
				Assert.IsEmpty(blogitems);
			}
		}

		[Test]
		public void SimpleAndSucessfulSituationUsingRootTransactionBoundaryStateless()
		{
			var service = container.Resolve<RootService>();

			service.SuccessFullCallStateless();

			Array blogs = service.FindAllStateless(typeof(Blog));
			Array blogitems = service.FindAllStateless(typeof(BlogItem));

			Assert.IsNotNull(blogs);
			Assert.IsNotNull(blogitems);
			Assert.AreEqual(1, blogs.Length);
			Assert.AreEqual(1, blogitems.Length);
		}

		[Test]
		public void CallWithExceptionStateless()
		{
			var service = container.Resolve<RootService>();

			try
			{
				service.CallWithExceptionStateless();
			}
			catch (NotSupportedException)
			{
			}

			// Ensure rollback happened

			Array blogs = service.FindAllStateless(typeof(Blog));
			Array blogitems = service.FindAllStateless(typeof(BlogItem));

			Assert.IsEmpty(blogs);
			Assert.IsEmpty(blogitems);
		}

		[Test]
		public void CallWithExceptionStateless2()
		{
			var service = container.Resolve<RootService>();

			try
			{
				service.CallWithExceptionStateless2();
			}
			catch (NotSupportedException)
			{
			}

			// Ensure rollback happened

			Array blogs = service.FindAllStateless(typeof(Blog));
			Array blogitems = service.FindAllStateless(typeof(BlogItem));

			Assert.IsEmpty(blogs);
			Assert.IsEmpty(blogitems);
		}
	}
}