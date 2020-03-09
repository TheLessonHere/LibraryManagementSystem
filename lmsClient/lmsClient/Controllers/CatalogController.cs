using LibraryData;
using lmsClient.Models.Catalog;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lmsClient.Controllers
{
    public class CatalogController : Controller
    {
        private ILibraryAsset _assets;
        private ICheckout _checkouts;
        public CatalogController(ILibraryAsset assets, ICheckout checkouts)
        {
            _assets = assets;
            _checkouts = checkouts;
        }

        public IActionResult Index()
        {
            // Get all the asset info
            var assetModels = _assets.GetAll();
            
            // Save the listing results into objects by selecting the data and listing it using our AssetIndexListingModel
            var listingResult = assetModels
                .Select(result => new AssetIndexListingModel
                {
                    Id = result.Id,
                    ImageUrl = result.ImageUrl,
                    Title = result.Title,
                    AuthorOrDirector = _assets.GetAuthorOrDirector(result.Id),
                    Type = _assets.GetType(result.Id),
                    DeweyCallNumber = _assets.GetDeweyIndex(result.Id)
                });

            // Save that into a copy of our container model "AssetIndexModel"
            var model = new AssetIndexModel()
            {
                Assets = listingResult
            };

            // Return the model to the view
            return View(model);
        }

        public IActionResult Detail(int id)
        {
            var asset = _assets.GetById(id);

            var currentHolds = _checkouts.GetCurrentHolds(id)
                .Select(asset => new AssetHoldModel
                {
                    HoldPlaced = _checkouts.GetCurrentHoldPlaced(asset.Id).ToString("d"),
                    PatronName = _checkouts.GetCurrentHoldPatronName(asset.Id)
                });

            var model = new AssetDetailModel
            {
                AssetId = id,
                Title = asset.Title,
                Type = _assets.GetType(id),
                Year = asset.Year,
                Cost = asset.Cost,
                Status = asset.Status.Name,
                ImageUrl = asset.ImageUrl,
                AuthorOrDirector = _assets.GetAuthorOrDirector(id),
                // Here a LibraryBranch Object is being returned so we only want the name on that object
                CurrentLocation = _assets.GetCurrentLocation(id).Name,
                DeweyCallNumber = _assets.GetDeweyIndex(id),
                ISBN = _assets.GetIsbn(id),
                CheckoutHistory = _checkouts.GetCheckoutHistory(id),
                LatestCheckout = _checkouts.GetLatestCheckout(id),
                PatronName = _checkouts.GetCurrentCheckoutPatron(id),
                CurrentHolds = currentHolds
            };

            return View(model);
        }
    }
}
