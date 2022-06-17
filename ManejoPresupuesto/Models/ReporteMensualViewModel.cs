namespace ManejoPresupuesto.Models
{
    public class ReporteMensualViewModel
    {
        public IEnumerable<ResultadoObtenerPorMes> transaccionesPorMes { get; set; }
        public decimal Ingresos => transaccionesPorMes.Sum(x => x.Ingreso);
        public decimal Gastos => transaccionesPorMes.Sum(x => x.Gasto);
        public decimal Total => Ingresos - Gastos;
        public int ano { get; set; }
    }
}
