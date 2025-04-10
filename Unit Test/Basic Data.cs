// <copyright file="Basic Relations.cs" company="Theta Rex, Inc.">
//    Copyright © 2025 - Theta Rex, Inc.  All Rights Reserved.
// </copyright>
// <author>Donald Airey</author>
namespace UnitTest
{
    using Microsoft.Extensions.Logging;
    using Moq;
    using UnitTest.Master;

    [TestClass]
    public sealed class BasicData
    {
        /// <summary>
        /// The account id.
        /// </summary>
        private static Guid AccountId = Guid.NewGuid();

        /// <summary>
        /// The account name.
        /// </summary>
        private static string AccountName = "Buffy Summers";

        [TestMethod]
        public async Task BasicCommit()
        {
            // Create the data model.
            var mockLogger = new Mock<ILogger<Fixture>>();
            var fixture = new Fixture(mockLogger.Object);

            // Create an account.
            var account = new Account
            {
                AccountId = BasicData.AccountId,
                Name = BasicData.AccountName,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();

                // Add the account.
                await account.EnterWriteLockAsync();
                await fixture.Accounts.AddAsync(account);

                // Commit the changes.
                asyncTransaction.Complete();
            }

            // Transaction to read the asset.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the table.
                await fixture.Accounts.EnterReadLockAsync();

                // Validate the account data.
                var foundAccount = fixture.Accounts.Find(BasicData.AccountId);
                ArgumentNullException.ThrowIfNull(foundAccount);
                await foundAccount.EnterReadLockAsync();
                Assert.AreEqual(foundAccount.Name, BasicData.AccountName);
            }
        }

        [TestMethod]
        public async Task BasicRollback()
        {
            // Create the data model.
            var mockLogger = new Mock<ILogger<Fixture>>();
            var fixture = new Fixture(mockLogger.Object);

            // Create an account.
            var account = new Account
            {
                AccountId = BasicData.AccountId,
                Name = BasicData.AccountName,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();

                // Add the account.
                await account.EnterWriteLockAsync();
                await fixture.Accounts.AddAsync(account);

                // Commit the changes.
                asyncTransaction.Complete();
            }

            // Update the data.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the table.
                await fixture.Accounts.EnterReadLockAsync();

                // Validate the account data.
                var foundAccount = fixture.Accounts.Find(BasicData.AccountId);
                ArgumentNullException.ThrowIfNull(foundAccount);
                await foundAccount.EnterWriteLockAsync();
                var cloneAccount = new Account(foundAccount);
                cloneAccount.Name = "Some dumb name";
                await fixture.Accounts.UpdateAsync(cloneAccount);

                // Should roll back here.
            }

            // Transaction to read the asset.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the table.
                await fixture.Accounts.EnterReadLockAsync();

                // Validate the account data.
                var foundAccount = fixture.Accounts.Find(BasicData.AccountId);
                ArgumentNullException.ThrowIfNull(foundAccount);
                await account.EnterReadLockAsync();
                Assert.AreEqual(account.Name, BasicData.AccountName);
            }
        }
    }
}