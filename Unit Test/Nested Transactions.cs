// <copyright file="Basic Relations.cs" company="Theta Rex, Inc.">
//    Copyright © 2025 - Theta Rex, Inc.  All Rights Reserved.
// </copyright>
// <author>Donald Airey</author>
namespace UnitTest
{
    using DataModelPrototype.Generated;

    [TestClass]
    public sealed class NestedTransactions
    {
        /// <summary>
        /// The simulated database.
        /// </summary>
        private readonly HashSet<Asset> assets = new HashSet<Asset>();

        [TestMethod]
        public async Task NestedCommit()
        {
            // Create the data model.
            var dataModel = new DataModel();
            dataModel.Assets.RowChanged += this.OnAssetRowChanged;

            // The outer transaction.
            var outerTransaction = new DataModelPrototype.AsyncTransaction();

            // The inner transaction.
            var innerTransaction = new DataModelPrototype.AsyncTransaction();

            // Create an asset in the inner transaction.
            await innerTransaction.WaitWriterAsync(dataModel.Assets);
            var asset2 = new Asset
            {
                Code = "AAPL",
                Name = "Apple Computer",
            };
            await innerTransaction.WaitWriterAsync(asset2);
            dataModel.Assets.Add(asset2);

            // Complete the inner transaction.
            innerTransaction.Complete();
            innerTransaction.Dispose();

            // The asset should not be in the simulated database until after the outer commit.
            Assert.IsFalse(this.assets.Contains(asset2));

            // Add the second asset after the first inner transaction has gone out of scope.
            var asset1 = new Asset
            {
                Code = "MSFT",
                Name = "Microsoft",
            };
            await outerTransaction.WaitWriterAsync(asset1);
            dataModel.Assets.Add(asset1);

            // Complete the outer transaction.
            outerTransaction.Complete();
            outerTransaction.Dispose();

            // Test that the simulated database has the expected records.
            Assert.IsTrue(this.assets.Contains(asset2));
            Assert.IsTrue(this.assets.Contains(asset1));
        }

        /// <summary>
        /// Handles a change to the asset table.
        /// </summary>
        /// <param name="sender">The object that originated the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnAssetRowChanged(object? sender, RowChangedEventArgs<Asset> e)
        {
            // Add the new asset to our simulated database.
            this.assets.Add(e.Data);
        }
    }
}