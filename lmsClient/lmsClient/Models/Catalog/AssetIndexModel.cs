using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace lmsClient.Models.Catalog
{
    public class AssetIndexModel
    {
        // Model for containing the assets listed using our AssetIndexListingModel
        public IEnumerable<AssetIndexListingModel> Assets { get; set; }
    }
}
