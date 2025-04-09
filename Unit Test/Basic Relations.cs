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
    public sealed class BasicRelations
    {
        /// <summary>
        /// The account id.
        /// </summary>
        private static Guid AccountId = Guid.NewGuid();

        /// <summary>
        /// The account name.
        /// </summary>
        private static string AccountName = "Buffy Summers";

        /// <summary>
        /// The asset id.
        /// </summary>
        private static Guid AssetId = Guid.NewGuid();

        /// <summary>
        /// The asset name.
        /// </summary>
        private static string AssetName = "AAPL Computer";

        [TestMethod]
        public async Task BasicCommit()
        {
            // Create the data model.
            var mockLogger = new Mock<ILogger<Fixture>>();
            var fixture = new Fixture(mockLogger.Object);

            // Create an account.
            var account = new Account
            {
                AccountId = BasicRelations.AccountId,
                Name = BasicRelations.AccountName,
            };

            // Create an asset.
            var asset = new Asset
            {
                AssetId = BasicRelations.AssetId,
                Name = BasicRelations.AssetName,
            };

            // Create a position.
            var position = new Position
            {
                AccountId = BasicRelations.AccountId,
                AssetId = BasicRelations.AssetId,
                Quantity = 100.0m,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Add the account.
                await account.EnterWriteLockAsync();
                await fixture.Accounts.AddAsync(account);

                // Add the asset.
                await asset.EnterWriteLockAsync();
                await fixture.Assets.AddAsync(asset);

                // Add the position.
                await position.EnterWriteLockAsync();
                await fixture.Positions.AddAsync(position);

                // Commit the changes.
                asyncTransaction.Complete();
            }

            // Transaction to read the asset.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the table.
                await fixture.Assets.EnterWriteLockAsync();

                // Validate the position.
                position = fixture.Positions.Find(BasicRelations.AccountId, BasicRelations.AssetId);
                Assert.IsNotNull(position);
                await position.EnterReadLockAsync();
                Asset.Equals(position.Account, account);
                Asset.Equals(position.Asset, asset);

                // Validate the account data.
                await position.EnterWriteLockAsync();
                Assert.AreEqual(account.Positions.Count, 1);
                account = position.Account;
                Assert.IsNotNull(account);
                await account.EnterReadLockAsync();
                Assert.AreEqual(account.Name, BasicRelations.AccountName);

                // Validate the asset data.
                await position.EnterWriteLockAsync();
                Asset.Equals(asset.Positions.Count, 1);
                asset = position.Asset;
                Assert.IsNotNull(asset);
                await asset.EnterReadLockAsync();
                Assert.AreEqual(asset.Name, BasicRelations.AssetName);
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
                AccountId = BasicRelations.AccountId,
                Name = BasicRelations.AccountName,
            };

            // Create an asset.
            var asset = new Asset
            {
                AssetId = BasicRelations.AssetId,
                Name = BasicRelations.AssetName,
            };

            // Create a position.
            var position = new Position
            {
                AccountId = BasicRelations.AccountId,
                AssetId = BasicRelations.AssetId,
                Quantity = 100.0m,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Accounts.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Add the account.
                await account.EnterWriteLockAsync();
                await fixture.Accounts.AddAsync(account);

                // Add the asset.
                await asset.EnterWriteLockAsync();
                await fixture.Assets.AddAsync(asset);

                // Add the position.
                await position.EnterWriteLockAsync();
                await fixture.Positions.AddAsync(position);

                // Don't commit the results.
            }

            // Transaction to read the data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Assets.EnterReadLockAsync();
                await fixture.Accounts.EnterReadLockAsync();
                await fixture.Positions.EnterReadLockAsync();

                // Assert that the tables are empty.
                Assert.AreEqual(fixture.Accounts.Count(), 0);
                Assert.AreEqual(fixture.Assets.Count(), 0);
                Assert.AreEqual(fixture.Positions.Count(), 0);

                // Assert that the relations are empty.
                Assert.IsNull(position.Account);
                Assert.IsNull(position.Asset);

                // Make sure we can't find the account.
                account = fixture.Accounts.Find(BasicRelations.AccountId);
                Assert.IsNull(account);

                // Make sure we can't find the asset.
                asset = fixture.Assets.Find(BasicRelations.AssetId);
                Assert.IsNull(asset);

                // Make sure we can't find the position.
                position = fixture.Positions.Find(BasicRelations.AccountId, BasicRelations.AssetId);
                Assert.IsNull(position);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ConstraintException))]
        public async Task ConstraintViolation()
        {
            // Create the data model.
            var mockLogger = new Mock<ILogger<Fixture>>();
            var fixture = new Fixture(mockLogger.Object);

            // Create an account.
            var account = new Account
            {
                AccountId = BasicRelations.AccountId,
                Name = BasicRelations.AccountName,
            };

            // Create an asset.
            var asset = new Asset
            {
                AssetId = BasicRelations.AssetId,
                Name = BasicRelations.AssetName,
            };

            // Create a position.
            var position = new Position
            {
                AccountId = BasicRelations.AccountId,
                AssetId = BasicRelations.AssetId,
                Quantity = 100.0m,
            };

            // Transaction to populate data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Add the account.
                await account.EnterWriteLockAsync();
                await fixture.Accounts.AddAsync(account);

                // Add the asset.
                await asset.EnterWriteLockAsync();
                await fixture.Assets.AddAsync(asset);

                // Add the position.
                await position.EnterWriteLockAsync();
                await fixture.Positions.AddAsync(position);

                // Commit the changes.
                asyncTransaction.Complete();
            }

            // New transaction to viloate the constraint.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Make sure we can't remove a parent record.
                var asset1 = fixture.Assets.Find(BasicRelations.AssetId);
                Assert.IsNotNull(asset1);
                await asset1.EnterWriteLockAsync();
                await fixture.Assets.RemoveAsync(asset1);

                // Remove the position.
                position = fixture.Positions.Find(BasicRelations.AccountId, BasicRelations.AssetId);
                Assert.IsNotNull(position);
                await position.EnterWriteLockAsync();
                await fixture.Positions.RemoveAsync(position);

                // We should now be able to remove the asset.
                await fixture.Assets.RemoveAsync(asset1);

                // Validate that it's no longer part of the model.
                asset1 = fixture.Assets.Find(BasicRelations.AssetId);
                Assert.IsNull(asset1);
            }
        }
    }
}