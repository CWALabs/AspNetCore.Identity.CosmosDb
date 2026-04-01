using AspNetCore.Identity.CosmosDb.Contracts;
using AspNetCore.Identity.CosmosDb.Stores;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AspNetCore.Identity.CosmosDb.Tests.Net9
{
    [TestClass]
    public class IdentityStoreBaseTests
    {
        [TestMethod]
        public void ThrowIfDisposed_WhenNotDisposed_DoesNotThrow()
        {
            var sut = new TestStoreBase(new Mock<IRepository>().Object);

            sut.InvokeThrowIfDisposed();
        }

        [TestMethod]
        public void ThrowIfDisposed_WhenDisposed_ThrowsObjectDisposedException()
        {
            var sut = new TestStoreBase(new Mock<IRepository>().Object);
            sut.MarkDisposed();

            Assert.ThrowsExactly<ObjectDisposedException>(() => sut.InvokeThrowIfDisposed());
        }

        [TestMethod]
        public void ProcessExceptions_ReturnsIdentityResultWith500Code()
        {
            var sut = new TestStoreBase(new Mock<IRepository>().Object);
            var result = sut.InvokeProcessExceptions(new InvalidOperationException("boom"));

            Assert.IsNotNull(result);
            Assert.IsFalse(result.Succeeded);
            Assert.AreEqual("500", result.Errors.Single().Code);
            Assert.AreEqual("boom", result.Errors.Single().Description);
        }

        private sealed class TestStoreBase : IdentityStoreBase
        {
            public TestStoreBase(IRepository repo) : base(repo)
            {
            }

            public void MarkDisposed()
            {
                _disposed = true;
            }

            public void InvokeThrowIfDisposed()
            {
                ThrowIfDisposed();
            }

            public IdentityResult InvokeProcessExceptions(Exception e)
            {
                return ProcessExceptions(e);
            }
        }
    }
}
