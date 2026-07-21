namespace SystemBase.BE.DTOs.Shared
{
    public class PaginationRequest
    {
        public int PageIndex { get; set; } = 1;
        
        private int _pageSize = 10;
        public int PageSize 
        { 
            get => _pageSize; 
            set => _pageSize = (value > 100) ? 100 : (value < 1 ? 1 : value); 
        }

        public string? Keyword { get; set; }
        public global::System.DateTime? StartDate { get; set; }
        public global::System.DateTime? EndDate { get; set; }
    }
}
