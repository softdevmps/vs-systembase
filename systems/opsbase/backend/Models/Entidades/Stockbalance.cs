namespace Backend.Models.Entidades
{
    public class Stockbalance
    {
        public int Id { get; set; }
        public int Resourceinstanceid { get; set; }
        public int Locationid { get; set; }
        public decimal Stockreal { get; set; }
        public decimal Stockreservado { get; set; }
        public decimal Stockdisponible { get; set; }
        public DateTime Createdat { get; set; }
        public DateTime Updatedat { get; set; }
    }
}
