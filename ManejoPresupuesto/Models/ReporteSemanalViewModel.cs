namespace ManejoPresupuesto.Models
{
    public class ReporteSemanalViewModel
    {
        public decimal Ingresos => transaccionesPorSemana.Sum(x => x.Ingresos);
        public decimal Gastos => transaccionesPorSemana.Sum(x => x.Gastos);
        public decimal Total => Ingresos - Gastos;
        public DateTime FechaReferencia { get; set; }
        public IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana { get; set; }
    }
}
