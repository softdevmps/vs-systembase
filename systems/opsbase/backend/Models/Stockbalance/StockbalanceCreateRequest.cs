namespace Backend.Models.Stockbalance
{
    public class StockbalanceCreateRequest
    {
        [System.ComponentModel.DataAnnotations.Required]
        public int Resourceinstanceid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public int Locationid { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Stockreal { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Stockreservado { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public decimal Stockdisponible { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Createdat { get; set; }
        [System.ComponentModel.DataAnnotations.Required]
        public DateTime Updatedat { get; set; }
    }
}
