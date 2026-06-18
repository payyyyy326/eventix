namespace Eventix.Share.Common.Models
{
    public class PaginationResponse<T> where T : class
    {
        public PaginationResponse()
        {
            DataList = new List<T>();
        }

        //response datalist
        public List<T> DataList { get; set; }

        //total overall pages
        public int TotalPages { get; set; }

        //total overall rows
        public int TotalRows { get; set; }

        //current page * page size
        public int CurrentPage { get; set; } = 1;

        //page size
        public int PageSize { get; set; } = 10;
    }


}
