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
        public async Task BasicAddCommit()
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

            // Create a model
            var model = new Model
            {
                ModelId = Guid.NewGuid(),
                Name = "Buffy Model",
            };

            // Transaction to populate the account and asset.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Models.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Add the model.
                await fixture.Models.AddAsync(model);

                // Add the account.
                account.ModelId = model.ModelId;
                await fixture.Accounts.AddAsync(account);

                // Add the asset.
                await fixture.Assets.AddAsync(asset);

                // Add the position.
                await fixture.Positions.AddAsync(position);

                // Commit the changes.
                asyncTransaction.Commit();
            }

            // Transaction to read the data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterReadLockAsync();
                await fixture.Assets.EnterReadLockAsync();
                await fixture.Models.EnterReadLockAsync();
                await fixture.Positions.EnterReadLockAsync();

                // Validate the foundPosition row.
                var foundPosition = fixture.Positions.Find(BasicRelations.AccountId, BasicRelations.AssetId);
                Assert.IsNotNull(foundPosition);
                await foundPosition.EnterReadLockAsync();
                Asset.Equals(foundPosition.Account, account);
                Asset.Equals(foundPosition.Asset, asset);

                // Validate the relationship to foundAccounts.
                var foundAccount = foundPosition.Account;
                Assert.IsNotNull(foundAccount);
                await foundAccount.EnterReadLockAsync();
                Assert.AreEqual(foundAccount.Positions.Count, 1);
                Assert.AreEqual(foundAccount.Name, account.Name);

                // Validate the relationship to foundAssets.
                var foundAsset = foundPosition.Asset;
                Assert.IsNotNull(foundAsset);
                await foundAsset.EnterReadLockAsync();
                Asset.Equals(foundAsset.Positions.Count, 1);
                Assert.AreEqual(foundAsset.Name, asset.Name);

                // Validate the model relationship to accounts.
                var foundModel = foundAccount.Model;
                Assert.IsNotNull(foundModel);
                Assert.AreEqual(foundModel.Accounts.Count, 1);
            }
        }

        [TestMethod]
        public async Task BasicAddRollback()
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

            // Create a model
            var model = new Model
            {
                ModelId = Guid.NewGuid(),
                Name = "Buffy Model",
            };

            // Transaction to populate the account and asset.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterWriteLockAsync();
                await fixture.Assets.EnterWriteLockAsync();
                await fixture.Models.EnterWriteLockAsync();
                await fixture.Positions.EnterWriteLockAsync();

                // Add the model.
                await fixture.Models.AddAsync(model);

                // Add the account.
                account.ModelId = model.ModelId;
                await fixture.Accounts.AddAsync(account);

                // Add the asset.
                await fixture.Assets.AddAsync(asset);

                // Add the position.
                await fixture.Positions.AddAsync(position);

                // Don't commit the changes.
            }

            // Transaction to read the data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Accounts.EnterReadLockAsync();
                await fixture.Assets.EnterReadLockAsync();
                await fixture.Models.EnterReadLockAsync();
                await fixture.Positions.EnterReadLockAsync();

                // Validate the foundPosition row.
                Assert.IsNull(fixture.Positions.Find(BasicRelations.AccountId, BasicRelations.AssetId));

                // Validate the relationship to foundAccounts.
                Assert.AreEqual(account.Positions.Count, 0);

                // Validate the relationship to foundAssets.
                Asset.Equals(asset.Positions.Count, 0);

                // Validate the model relationship to accounts.
                Assert.IsNull(account.Model);
            }
        }

        [TestMethod]
        public async Task BasicRemoveCommit()
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

            // Transaction to populate the data model.
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
                asyncTransaction.Commit();
            }

            // Transaction to remove the position.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Positions.EnterWriteLockAsync();

                // Remove the position.
                var foundRow = fixture.Positions.Find(position.AccountId, position.AssetId);
                ArgumentNullException.ThrowIfNull(foundRow);
                await fixture.Positions.RemoveAsync(foundRow);

                // Assert that the row no longer exists in the data model.
                Assert.IsNull(fixture.Positions.Find(position.AccountId, position.AssetId));

                // Commit the changes.
                asyncTransaction.Commit();
            }

            // Transaction to read the data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Positions.EnterReadLockAsync();

                // Assert that the row has been removed from the data model.
                Assert.IsNull(fixture.Positions.Find(position.AccountId, position.AssetId));
                Assert.AreEqual(fixture.Positions.DeletedRows.Count(), 1);
            }
        }

        [TestMethod]
        public async Task BasicRemoveRollback()
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

            // Transaction to populate the data model.
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
                asyncTransaction.Commit();
            }

            // Transaction to remove the position.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Positions.EnterWriteLockAsync();

                // Remove the position.
                var foundRow = fixture.Positions.Find(position.AccountId, position.AssetId);
                ArgumentNullException.ThrowIfNull(foundRow);
                await fixture.Positions.RemoveAsync(foundRow);

                // Assert that the row no longer exists in the data model.
                Assert.IsNull(fixture.Positions.Find(position.AccountId, position.AssetId));

                // Rollback the transaction.
            }

            // Transaction to read the data model.
            using (var asyncTransaction = new AsyncTransaction())
            {
                // Lock the tables.
                await fixture.Positions.EnterReadLockAsync();

                // Assert that the row has been restored to the data model.
                Assert.IsNotNull(fixture.Positions.Find(position.AccountId, position.AssetId));
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
                asyncTransaction.Commit();
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