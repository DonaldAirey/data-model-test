// <copyright file="Basic Relations.cs" company="Theta Rex, Inc.">
//    Copyright © 2025 - Theta Rex, Inc.  All Rights Reserved.
// </copyright>
// <author>Donald Airey</author>
namespace UnitTest
{
    using System.Transactions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using UnitTest.Master;

    [TestClass]
    public sealed class Deadlock
    {
        /// <summary>
        /// The AAPL id.
        /// </summary>
        private static Guid AaplId = Guid.NewGuid();

        /// <summary>
        /// The AAPL name.
        /// </summary>
        private static string AaplName = "AAPL Inc.";

        /// <summary>
        /// The META id.
        /// </summary>
        private static Guid MetaId = Guid.NewGuid();

        /// <summary>
        /// The META name.
        /// </summary>
        private static string MetaName = "Meta Platforms Inc.";

        /// <summary>
        /// The MSFT id.
        /// </summary>
        private static Guid MsftId = Guid.NewGuid();

        /// <summary>
        /// The MSFT name.
        /// </summary>
        private static string MsftName = "Microsoft Corp.";

        /// <summary>
        /// The mock logger.
        /// </summary>
        private static Mock<ILogger<Fixture>> mockLogger = new Mock<ILogger<Fixture>>();

        /// <summary>
        /// The fixture.
        /// </summary>
        private readonly Fixture fixture = new Fixture(Deadlock.mockLogger.Object);

        /// <summary>
        /// Used to coordinate tasks.
        /// </summary>
        private readonly SemaphoreSlim semaphoreSlim = new SemaphoreSlim(0, 1);

        [TestMethod]
        public async Task BasicDeadlock()
        {
            // Create the AAPL assets.
            var aapl = new Asset
            {
                AssetId = Deadlock.AaplId,
                Name = Deadlock.AaplName,
            };

            // Create an AAPL Quote.
            var aaplQuote = new Quote
            {
                AssetId = Deadlock.AaplId,
                Last = 180.00m,
            };

            // Create the MSFT assets.
            var msft = new Asset
            {
                AssetId = Deadlock.MsftId,
                Name = Deadlock.MsftName,
            };

            // Create a MSFT Quote.
            var msftQuote = new Quote
            {
                AssetId = Deadlock.MsftId,
                Last = 350.00m,
            };

            // Create the Meta assets.
            var meta = new Asset
            {
                AssetId = Deadlock.MetaId,
                Name = Deadlock.MetaName,
            };

            // Create a Meta Quote.
            var metaQuote = new Quote
            {
                AssetId = Deadlock.MetaId,
                Last = 14.00m,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await this.fixture.Accounts.EnterWriteLockAsync();
                await this.fixture.Assets.EnterWriteLockAsync();
                await this.fixture.Quotes.EnterWriteLockAsync();

                // Add the assets.
                await this.fixture.Assets.AddAsync(aapl);
                await this.fixture.Assets.AddAsync(meta);
                await this.fixture.Assets.AddAsync(msft);

                // Add the quotes.
                await this.fixture.Quotes.AddAsync(aaplQuote);
                await this.fixture.Quotes.AddAsync(metaQuote);
                await this.fixture.Quotes.AddAsync(msftQuote);

                // Commit the changes.
                asyncTransaction.Commit();
            }

            // Spawn the deadlocking threads.
            var readQuoteTask = Task.Run(ReadQuotesAsync);
            var writeQuoteTask = Task.Run(WriteQuotesAsync);

            try
            {
                // Wait for the deadlocked tasks to be terminated.  Either of these exceptions are possible depending on which task finishes first.
                // Any other exception will fail the test.
                await Task.WhenAll(readQuoteTask, writeQuoteTask);
                Assert.Fail("We should not complete the tests without an exception.");
            }
            catch (OperationCanceledException)
            {
            }

            // Verify that the transations were never committed.
            Assert.AreEqual(180.00m, aaplQuote.Last);
            Assert.AreEqual(350.00m, msftQuote.Last);
        }

        /// <summary>
        /// Read the quotes.
        /// </summary>
        /// <returns></returns>
        private async Task ReadQuotesAsync()
        {
            // Transaction to populate data model.
            using var asyncTransaction = new AsyncTransaction();

            // Lock the tables.
            await this.fixture.Quotes.EnterReadLockAsync();

            // Attempt to read the AAPL quotes.
            var aaplQuote = this.fixture.Quotes.Find(Deadlock.AaplId);
            Assert.IsNotNull(aaplQuote);
            await aaplQuote.EnterReadLockAsync();

            // Wait for the signal.
            await this.semaphoreSlim.WaitAsync();

            // Attempt to read the MSFT quote. Should deadlock.
            var msftQuote = this.fixture.Quotes.Find(Deadlock.MsftId);
            Assert.IsNotNull(msftQuote);
            await msftQuote.EnterReadLockAsync();

            // Should take an exception waiting for the lock.
            Assert.Fail("After a deadlock, execution of the task should end.");
        }

        /// <summary>
        /// Write the quotes.
        /// </summary>
        /// <returns></returns>
        private async Task WriteQuotesAsync()
        {
            // Transaction to populate data model.
            using var asyncTransaction = new AsyncTransaction();

            // Lock the tables.
            await this.fixture.Quotes.EnterReadLockAsync();

            // Attempt to write the MSFT quote.
            var msftQuote = this.fixture.Quotes.Find(Deadlock.MsftId);
            Assert.IsNotNull(msftQuote);
            var msftClone = new Quote(msftQuote);
            msftClone.Last = 1.00m;
            await this.fixture.Quotes.UpdateAsync(msftClone);

            // Allow the other task to run.
            this.semaphoreSlim.Release();
            await Task.Yield();

            // Attempt to write the AAPL quotes.  Should deadlock with the reading task, but will survive longer because we started later, so the
            // transaction should still be alive when we attempt to update.
            var aaplQuote = this.fixture.Quotes.Find(Deadlock.AaplId);
            Assert.IsNotNull(aaplQuote);
            var applClone = new Quote(aaplQuote);
            applClone.Last = 1.00m;
            await this.fixture.Quotes.UpdateAsync(applClone);

            // Should take an exception waiting for the lock.
            Assert.Fail("After a deadlock, execution of the task should end.");

            // Commit the results.
            asyncTransaction.Commit();
        }
    }
}