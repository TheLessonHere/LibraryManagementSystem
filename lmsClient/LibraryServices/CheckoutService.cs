using LibraryData;
using LibraryData.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryServices
{
    public class CheckoutService : ICheckout
    {
        private LibraryContext _context;
        public CheckoutService(LibraryContext context)
        {
            _context = context;
        }
        // Create methods
        public void Add(Checkouts newCheckout)
        {
            _context.Add(newCheckout);
            _context.SaveChanges();
        }

        // Read methods
        // Collection getters
        public IEnumerable<Checkouts> GetAll()
        {
            return _context.Checkouts;
        }
        public IEnumerable<CheckoutHistory> GetCheckoutHistory(int id)
        {
            return _context.CheckoutHistories
                .Include(history => history.LibraryAsset)
                .Include(history => history.LibraryCard)
                .Where(history => history.LibraryAsset.Id == id);
        }
        public IEnumerable<Holds> GetCurrentHolds(int id)
        {
            return _context.Holds
                .Include(hold => hold.LibraryAsset)
                .Where(hold => hold.LibraryAsset.Id == id);
        }
        // Object getters
        public Checkouts GetById(int checkoutId)
        {
            return GetAll()
                .FirstOrDefault(checkout => checkout.Id == checkoutId);
        }
        public Checkouts GetLatestCheckout(int assetId)
        {
            return _context.Checkouts
                .Where(checkout => checkout.LibraryAsset.Id == assetId)
                .OrderByDescending(checkout => checkout.Since)
                .FirstOrDefault();
        }
        // String getters
        public string GetCurrentHoldPatronName(int holdId)
        {
            var hold = _context.Holds
                .Include(hold => hold.LibraryAsset)
                .Include(hold => hold.LibraryCard)
                .FirstOrDefault(hold => hold.Id == holdId);

            // ? operator represents a null conditional. This will replace our variables if
            // they are null instead of throwing an exception
            var cardId = hold?.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(patron => patron.LibraryCard)
                .FirstOrDefault(patron => patron.LibraryCard.Id == cardId);

            return patron?.FirstName + " " + patron?.LastName;
        }
        public string GetCurrentCheckoutPatron(int assetId)
        {
            var checkout = GetCheckoutByAssetId(assetId);
            if (checkout == null)
            {
                return "";
            };

            var cardId = checkout.LibraryCard.Id;

            var patron = _context.Patrons
                .Include(patron => patron.LibraryCard)
                .FirstOrDefault(patron => patron.LibraryCard.Id == cardId);

            return patron.FirstName + " " + patron.LastName;
        }
        // Boolean getters
        public bool IsCheckedOut(int assetId)
        {
            return _context.Checkouts
                .Where(checkout => checkout.LibraryAsset.Id == assetId)
                .Any();
        }
        // DateTime getters
        public DateTime GetCurrentHoldPlaced(int holdId)
        {
            return _context.Holds
                .Include(hold => hold.LibraryAsset)
                .Include(hold => hold.LibraryCard)
                .FirstOrDefault(hold => hold.Id == holdId)
                .HoldPlaced;
        }

        // Update methods
        public void CheckOutItem(int assetId, int libraryCardId)
        {
            if (IsCheckedOut(assetId)) return;

            // Update library asset status to Checked out
            var item = _context.LibraryAssets
                .FirstOrDefault(asset => asset.Id == assetId);

            UpdateAssetStatus(assetId, "Checked Out");

            // Grab the library card and date time to create a new checkout
            var libraryCard = _context.LibraryCards
                .Include(card => card.Checkouts)
                .FirstOrDefault(card => card.Id == libraryCardId);

            var now = DateTime.Now;

            // Create a new checkout and add it to the db
            var checkout = new Checkouts
            {
                LibraryAsset = item,
                LibraryCard = libraryCard,
                Since = now,
                Until = GetDefaultCheckoutTime(now)
            };

            _context.Add(checkout);

            // Create a new checkout history and add that as well
            var checkoutHistory = new CheckoutHistory
            {
                CheckedOut = now,
                LibraryAsset = item,
                LibraryCard = libraryCard
            };

            _context.Add(checkoutHistory);
            _context.SaveChanges();
        }
        public void CheckInItem(int assetId)
        {
            var now = DateTime.Now;

            var item = _context.LibraryAssets
                .FirstOrDefault(asset => asset.Id == assetId);

            // Remove any existing checkouts on the asset
            RemoveExistingCheckouts(assetId);

            // Close any existing checkout history
            CloseExistingCheckoutHistory(assetId, now);

            // Look for existing holds on the asset
            var currentHolds = _context.Holds
                .Include(hold => hold.LibraryAsset)
                .Include(hold => hold.LibraryCard)
                .Where(hold => hold.LibraryAsset.Id == assetId);

            // If holds exist, checkout the item to the
            //   librarycard with the earliest hold
            if (currentHolds.Any())
            {
                CheckoutToEarliestHold(assetId, currentHolds);
                return;
            }

            // Otherwise, change status to available
            UpdateAssetStatus(assetId, "Available");

            _context.SaveChanges();
        }
        public void PlaceHold(int assetId, int libraryCardId)
        {
            var now = DateTime.Now;

            var asset = _context.LibraryAssets
                .Include(asset => asset.Status)
                .FirstOrDefault(asset => asset.Id == assetId);

            var card = _context.LibraryCards
                .FirstOrDefault(card => card.Id == libraryCardId);

            if (asset.Status.Name == "Available")
            {
                UpdateAssetStatus(assetId, "On Hold");
            }

            var hold = new Holds
            {
                HoldPlaced = now,
                LibraryAsset = asset,
                LibraryCard = card
            };

            _context.Add(hold);
            _context.SaveChanges();
        }
        public void MarkLost(int assetId)
        {
            UpdateAssetStatus(assetId, "Lost");

            _context.SaveChanges();
        }
        public void MarkFound(int assetId)
        {
            var now = DateTime.Now;

            UpdateAssetStatus(assetId, "Available");

            RemoveExistingCheckouts(assetId);

            CloseExistingCheckoutHistory(assetId, now);

            _context.SaveChanges();
        }

        // Private helper methods
        private Checkouts GetCheckoutByAssetId(int assetId)
        {
            return _context.Checkouts
                .Include(checkout => checkout.LibraryAsset)
                .Include(checkout => checkout.LibraryCard)
                .FirstOrDefault(checkout => checkout.LibraryAsset.Id == assetId);
        }
        private DateTime GetDefaultCheckoutTime(DateTime now)
        {
            return now.AddDays(30);
        }
        private void CheckoutToEarliestHold(int assetId, IQueryable<Holds> currentHolds)
        {
            var earliestHold = currentHolds
                .OrderBy(holds => holds.HoldPlaced)
                .FirstOrDefault();

            var card = earliestHold.LibraryCard;

            _context.Remove(earliestHold);
            _context.SaveChanges();
            CheckOutItem(assetId, card.Id);
        }
        private void UpdateAssetStatus(int assetId, string statusType)
        {
            var item = _context.LibraryAssets
                .Include(asset => asset.Status)
                .FirstOrDefault(asset => asset.Id == assetId);

            _context.Update(item);

            item.Status = _context.Statuses
                .FirstOrDefault(status => status.Name == statusType);
        }
        private void CloseExistingCheckoutHistory(int assetId, DateTime now)
        {
            var history = _context.CheckoutHistories
                .FirstOrDefault(history => history.LibraryAsset.Id == assetId
                && history.CheckedIn == null);

            if (history != null)
            {
                _context.Update(history);
                history.CheckedIn = now;
            }
        }
        private void RemoveExistingCheckouts(int assetId)
        {
            var checkout = _context.Checkouts
                .FirstOrDefault(checkout => checkout.LibraryAsset.Id == assetId);

            if (checkout != null)
            {
                _context.Remove(checkout);
            }
        }
    }
}
