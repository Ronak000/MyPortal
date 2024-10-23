namespace MyPortal.Models
{
    public class ODataResponse<T>
    {
        public List<T> Value { get; set; }
    }
}
