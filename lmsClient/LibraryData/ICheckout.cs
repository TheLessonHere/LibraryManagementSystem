using LibraryData.Models;
using System;
using System.Collections.Generic;

namespace LibraryData
{
    public interface ICheckout
    {
        // Create methods
        void Add(Checkouts newCheckout);
        // Read methods
        // Collection getters
        IEnumerable<Checkouts> GetAll();
        IEnumerable<CheckoutHistory> GetCheckoutHistory(int id);
        IEnumerable<Holds> GetCurrentHolds(int id);
        // Object getters
        Checkouts GetById(int checkoutId);
        Checkouts GetLatestCheckout(int assetId);
        // String getters
        string GetCurrentHoldPatronName(int id);
        string GetCurrentCheckoutPatron(int assetId);
        // Boolean getters
        bool IsCheckedOut(int id);
        // DateTime getters
        DateTime GetCurrentHoldPlaced(int id);
        // Update methods
        void CheckOutItem(int assetId, int libraryCardId);
        void CheckInItem(int assetId);
        void PlaceHold(int assetId, int libraryCardId);
        void MarkLost(int assetId);
        void MarkFound(int assetId);
    }
}
