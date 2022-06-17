using ManejoPresupuesto.Models;

namespace ManejoPresupuesto.Servicios
{
    public interface IServicioReportes
    {
        Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuarioId, int mes, int ano, dynamic ViewBag);
        Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetallasPorCuenta(int usuarioId, int cuentaId, int mes, int ano, dynamic ViewBag);
        Task<ReporteTransaccionesDetalladas> ReporteTransaccionesDetalladas(int usuarioId, int mes, int ano, dynamic ViewBag);
    }
    public class ServicioReportes : IServicioReportes
    {
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly HttpContext httpContext;

        public ServicioReportes(IRepositorioTransacciones transacciones, IHttpContextAccessor httpContextAccessor)
        {
            this.repositorioTransacciones = transacciones;
            this.httpContext = httpContextAccessor.HttpContext;
        }

        public async Task<IEnumerable<ResultadoObtenerPorSemana>> ObtenerReporteSemanal(int usuarioId,
            int mes, int ano, dynamic ViewBag)
        {
            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, ano);

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {

                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };
            AsingarValoresAlViewBag(ViewBag, fechaInicio);
            var modelo = await repositorioTransacciones.ObtenerPorSemana(parametro);
            return modelo;

        }

        public async Task<ReporteTransaccionesDetalladas> ReporteTransaccionesDetalladas(int usuarioId,
         int mes, int ano, dynamic ViewBag)
        {
            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, ano);

            var parametro = new ParametroObtenerTransaccionesPorUsuario()
            {
                
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(parametro);

            var modelo = GenerarReporteTransaccionesDetalladas(fechaInicio, fechaFin, transacciones);
            AsingarValoresAlViewBag(ViewBag, fechaInicio);

            return modelo;
        }
        public async Task<ReporteTransaccionesDetalladas> ObtenerReporteTransaccionesDetallasPorCuenta(int usuarioId, int cuentaId,
            int mes, int ano, dynamic ViewBag)
        {
            (DateTime fechaInicio, DateTime fechaFin) = GenerarFechaInicioYFin(mes, ano);

            var obtenerTransaccionesPorCuenta = new ObtenerTransaccionesPorCuenta()
            {
                CuentaId = cuentaId,
                UsuarioId = usuarioId,
                FechaInicio = fechaInicio,
                FechaFin = fechaFin
            };


            var transacciones = await repositorioTransacciones
                                  .ObtenerPorCuentaId(obtenerTransaccionesPorCuenta);

            var modelo = GenerarReporteTransaccionesDetalladas(fechaInicio, fechaFin, transacciones);
            AsingarValoresAlViewBag(ViewBag, fechaInicio);

            return modelo;
        }

        private void AsingarValoresAlViewBag(dynamic ViewBag, DateTime fechaInicio)
        {
            ViewBag.mesAnterior = fechaInicio.AddMonths(-1).Month;
            ViewBag.anoAnterior = fechaInicio.AddYears(-1).Year;
            ViewBag.mesPosterior = fechaInicio.AddMonths(1).Month;
            ViewBag.anoPosterior = fechaInicio.AddMonths(1).Year;
            ViewBag.urlRetorno = httpContext.Request.Path + httpContext.Request.QueryString;
        }

        private static ReporteTransaccionesDetalladas GenerarReporteTransaccionesDetalladas(DateTime fechaInicio, DateTime fechaFin, IEnumerable<Transaccion> transacciones)
        {
            var modelo = new ReporteTransaccionesDetalladas();


            var transaccionesPorFecha = transacciones.OrderByDescending(x => x.FechaTransaccion)
                                        .GroupBy(x => x.FechaTransaccion)
                                         .Select(grupo => new ReporteTransaccionesDetalladas.TransaccionesPorFecha()
                                         {
                                             FechaTransaccion = grupo.Key,
                                             Transacciones = grupo.AsEnumerable()
                                         });


            modelo.TransaccionesAgrupadas = transaccionesPorFecha;
            modelo.FechaInicio = fechaInicio;
            modelo.FechaFin = fechaFin;
            return modelo;
        }

        private (DateTime fechaInicio, DateTime fechaFin) GenerarFechaInicioYFin(int mes, int ano)
        {
            DateTime fechaInicio;
            DateTime fechaFin;

            if (mes <= 0 || mes > 12 || ano <= 1900)
            {
                var hoy = DateTime.Today;
                fechaInicio = new DateTime(hoy.Year, hoy.Month, 1);
            }
            else
            {
                fechaInicio = new DateTime(ano, mes, 1);
            }

            fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

            return (fechaInicio, fechaFin);
        }
    }
}
