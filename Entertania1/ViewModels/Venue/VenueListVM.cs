using Core.Models;
using System.Collections.Generic;

namespace Entertania1.ViewModels.Venue
{
    public class VenueListVM
    {
        public IEnumerable<Core.Models.Venue>? Venues { get; set; }

        // Filtering
        public string? SearchTerm { get; set; }

        // Sorting
        public string? SortOrder { get; set; }

        // Pagination
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
