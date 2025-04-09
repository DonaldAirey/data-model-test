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
        private static string AaplName = "AAPL Computer";

        /// <summary>
        /// The MSFT id.
        /// </summary>
        private static Guid MsftId = Guid.NewGuid();

        /// <summary>
        /// The MSFT name.
        /// </summary>
        private static string MsftName = "Microsoft Corp";

        /// <summary>
        /// The mock logger.
        /// </summary>
        private static Mock<ILogger<Fixture>> mockLogger = new Mock<ILogger<Fixture>>();

        /// <summary>
        /// The fixture.
        /// </summary>
        private readonly Fixture fixture = new Fixture(Deadlock.mockLogger.Object);

        [TestMethod]
        public async Task BasicDeadlock()
        {
            System.Diagnostics.Debug.WriteLine("Started test");

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

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await this.fixture.Accounts.EnterWriteLockAsync();
                await this.fixture.Assets.EnterWriteLockAsync();
                await this.fixture.Quotes.EnterWriteLockAsync();

                // Add the assets.
                await aapl.EnterWriteLockAsync();
                await msft.EnterWriteLockAsync();
                await this.fixture.Assets.AddAsync(aapl);
                await this.fixture.Assets.AddAsync(msft);

                // Add the quotes.
                await aaplQuote.EnterWriteLockAsync();
                await msftQuote.EnterWriteLockAsync();
                await this.fixture.Quotes.AddAsync(aaplQuote);
                await this.fixture.Quotes.AddAsync(msftQuote);

                // Commit the changes.
                asyncTransaction.Complete();
            }

            // Spawn the deadlocking threads.
            var readQuoteTask = Task.Run(ReadQuotesAsync);
            var writeQuoteTask = Task.Run(WriteQuotesAsync);

            // Wait for the deadlocked tasks to be terminated.  Either of these exceptions are possible depending on which task finishes first.  Any
            // other exception will fail the test.
            try
            {
                await Task.WhenAll(readQuoteTask, writeQuoteTask);
                Assert.Fail("We should not complete the tests without an exception.");
            }
            catch (OperationCanceledException)
            {
            }
            catch (TransactionAbortedException)
            {
            }
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

            // Allow the other task to run.
            await Task.Yield();

            // Attempt to read the MSFT quote. Should deadlock.
            var msftQuote = this.fixture.Quotes.Find(Deadlock.MsftId);
            Assert.IsNotNull(msftQuote);
            await msftQuote.EnterReadLockAsync();
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
            await msftQuote.EnterWriteLockAsync();
            msftQuote.Last = 1.50m;

            // Allow the other task to run.
            await Task.Yield();

            // Attempt to write the AAPL quotes. Should deadlock.
            var aaplQuote = this.fixture.Quotes.Find(Deadlock.AaplId);
            Assert.IsNotNull(aaplQuote);
            await aaplQuote.EnterWriteLockAsync();
            aaplQuote.Last = 1.20m;

            // Commit the results.
            asyncTransaction.Complete();
        }
    }
}