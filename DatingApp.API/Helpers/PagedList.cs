using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DatingApp.API.Helpers
{
    public class PagedList<T> : List<T>
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

        public PagedList(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            CurrentPage = pageNumber;
            // i.e. 13 users -> 13/ 5 pages = 3, because its going to show 5,5 then 3 totaling 13
            TotalPages = (int)Math.Ceiling(count/ (double) pageSize);
            this.AddRange(items);
        }

        public static async Task<PagedList<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize) 
        {
            // total count of items in source using CountAsync
            // i.e. for users this will be the count of ALL users
            var count = await source.CountAsync();
            //  get items from the source of items passed in this method and we use the paging info (pageNumber, pageSize) to work out what we are returning back
            // i.e. for 13 users where only 5 displayed per page, and we want page 2 -> ((2 - 1) * 5) = 5, so we ignore the first 5 users and display the next 5
            var items = await source.Skip((pageNumber -1) * pageSize).Take(pageSize).ToListAsync();

            return new PagedList<T>(items, count, pageNumber, pageSize); 
        }

    }
}