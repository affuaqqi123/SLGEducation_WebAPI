using System.ComponentModel.DataAnnotations;

namespace WebApi.Model
{
    public class StoreModel
    {
        [Key]
        public int StoreID { get; set; }
        public string StoreLocation { get; set; }
    }

}