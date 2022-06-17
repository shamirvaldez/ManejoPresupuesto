using AutoMapper;
using ClosedXML.Excel;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Data;

namespace ManejoPresupuesto.Controllers
{
    public class TransaccionesController : Controller
    {
        private readonly IServiciosUsuarios servicioUsuarios;
        private readonly IRepostiorioCuentas repositorioCuentas;
        private readonly IRepositorioCategorias repositorioCategorias;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IMapper mapper;
        private readonly IServicioReportes servicioReportes;

        public TransaccionesController(IServiciosUsuarios servicioUsuarios, IRepostiorioCuentas repositorioCuentas,
            IRepositorioCategorias repositorioCategorias, IRepositorioTransacciones repositorioTransacciones,
            IMapper mapper , IServicioReportes servicioReportes)
        {
            this.servicioUsuarios = servicioUsuarios;
            this.repositorioCuentas = repositorioCuentas;
            this.repositorioCategorias = repositorioCategorias;
            this.repositorioTransacciones = repositorioTransacciones;
            this.mapper = mapper;
            this.servicioReportes = servicioReportes;
        }

        public async Task<IActionResult>  Index(int mes, int ano)
        {

            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var modelo = await  servicioReportes.ReporteTransaccionesDetalladas(usuarioId, mes, ano, ViewBag);

            return View(modelo);
        }

        public IActionResult Diario()
        {
            return View();
        }

        public async Task<IActionResult> Semanal(int mes, int ano)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            IEnumerable<ResultadoObtenerPorSemana> transaccionesPorSemana =
                await servicioReportes.ObtenerReporteSemanal(usuarioId, mes, ano, ViewBag);

            var agrupados = transaccionesPorSemana.GroupBy(x => x.Semana).Select(x => new ResultadoObtenerPorSemana
            {
                Semana = x.Key,
                Ingresos = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso).Select(x => x.Monto).FirstOrDefault(),
                Gastos = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                    .Select(x => x.Monto).FirstOrDefault()

            }).ToList();

            if(ano == 0 || mes == 0)
            {
                var hoy = DateTime.Today;
                ano = hoy.Year;
                mes = hoy.Month;
            }

            var fechaReferencia = new DateTime(ano, mes, 1);

            var diaDelMes = Enumerable.Range(1, fechaReferencia.AddMonths(1).AddDays(-1).Day);

            var diasSegmentados = diaDelMes.Chunk(7).ToList();


            for (int i = 0; i < diasSegmentados.Count(); i++)
            {
                var semana = i + 1;
                var fechaInicio = new DateTime(ano, mes, diasSegmentados[i].First());
                var fechaFin = new DateTime(ano, mes, diasSegmentados[i].Last());
                var grupoSemana = agrupados.FirstOrDefault(x => x.Semana == semana);

                if (grupoSemana is null)
                {
                    
                    agrupados.Add(new ResultadoObtenerPorSemana()
                    {
                        Semana = semana,
                        FechaInicio = fechaInicio,
                        FechaFin = fechaFin
                    });
                }
                else
                {
                    grupoSemana.FechaInicio = fechaInicio;
                    grupoSemana.FechaFin = fechaFin;
                }
            }

            agrupados = agrupados.OrderByDescending(x => x.Semana).ToList();

            var modelo = new ReporteSemanalViewModel();
            modelo.transaccionesPorSemana = agrupados;
            modelo.FechaReferencia = fechaReferencia;
            return View(modelo);
        }

        public async Task<IActionResult> Mensual(int ano)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            if(ano == 0)
            {
                ano = DateTime.Today.Year;
            }
            var transaccionesPorMes = await repositorioTransacciones.ObtenerPorMes(usuarioId, ano);

            var transaccioneAgrupadas = transaccionesPorMes.GroupBy(x => x.Mes)
                .Select(x => new ResultadoObtenerPorMes()
                {
                    Mes = x.Key,
                    Ingreso = x.Where(x => x.TipoOperacionId == TipoOperacion.Ingreso)
                    .Select(x => x.Monto).FirstOrDefault(),
                    Gasto = x.Where(x => x.TipoOperacionId == TipoOperacion.Gasto)
                    .Select(x => x.Monto).FirstOrDefault(),
                }).ToList();

            for (int mes = 1; mes <= 12; mes++)
            {
                var transaccion = transaccioneAgrupadas.FirstOrDefault(x => x.Mes == mes);
                var fechaReferencia = new DateTime(ano, mes, 1);

                if(transaccion is null)
                {
                    transaccioneAgrupadas.Add(new ResultadoObtenerPorMes()
                    {
                        Mes = mes,
                        FechaReferencia = fechaReferencia
                    });
                }
                else
                {
                    transaccion.FechaReferencia = fechaReferencia;
                }
            }

            transaccioneAgrupadas = transaccioneAgrupadas.OrderByDescending(x => x.Mes).ToList();

            var modelo = new ReporteMensualViewModel();

            modelo.ano = ano;
            modelo.transaccionesPorMes = transaccioneAgrupadas;

            return View(modelo);
        }


        public IActionResult ExcelReporte()
        {
            return View();
        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelPorMes(int mes, int ano)
        {
            var fechaInicio = new DateTime(ano, mes, 1);
            var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("MMM yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);
        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelPorAño(int ano)
        {
            var fechaInicio = new DateTime(ano, 1, 1);
            var fechaFin = fechaInicio.AddYears(1).AddDays(-1);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {fechaInicio.ToString("yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);
        }

        [HttpGet]
        public async Task<FileResult> ExportarExcelTodo()
        {
            var fechaInicio = DateTime.Today.AddYears(-100);
            var fechaFin = DateTime.Today.AddYears(1000);
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var transacciones = await repositorioTransacciones.ObtenerPorUsuarioId(
                new ParametroObtenerTransaccionesPorUsuario
                {
                    UsuarioId = usuarioId,
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin
                });

            var nombreArchivo = $"Manejo Presupuesto - {DateTime.Today.ToString("dd-MM-yyyy")}.xlsx";
            return GenerarExcel(nombreArchivo, transacciones);
        }
        private FileResult GenerarExcel(string nombreArchivo, IEnumerable<Transaccion> transacciones)
        {
            DataTable datatable = new DataTable("Transacciones");
            datatable.Columns.AddRange(new DataColumn[]
            {
                new DataColumn("Fecha"),
                new DataColumn("Cuenta"),
                new DataColumn("Categoria"),
                new DataColumn("Nota"),
                new DataColumn("Monto"),
                new DataColumn("Ingreso/Gasto"),
            });

            foreach(var transaccion in transacciones)
            {
                datatable.Rows.Add(transaccion.FechaTransaccion,
                                  transaccion.Cuenta,
                                  transaccion.Categoria,
                                  transaccion.Nota,
                                  transaccion.Monto,
                                  transaccion.TipoOperacionId);
            }

            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(datatable);

                using(MemoryStream stream = new MemoryStream())
                {
                    wb.SaveAs(stream);
                    return File(stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    nombreArchivo);
                }
            }
        }
        public IActionResult Calendario()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = mapper.Map<TransaccionActualizacionViewModel>(transaccion);

            modelo.MontoAnterior = modelo.Monto;

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.MontoAnterior = modelo.Monto * -1;
            }

            modelo.CuentaAnteriorId = transaccion.CuentaId;
            modelo.Categorias = await ObtenerCategorias(usuarioId, transaccion.TipoOperacionId);
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.urlRetorno = urlRetorno;

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(TransaccionActualizacionViewModel modelo)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            ModelState.Remove("Categorias");
            ModelState.Remove("Categoria");
            ModelState.Remove("Cuenta");
            ModelState.Remove("Cuentas");

            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuenta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);

            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var transaccion = mapper.Map<Transaccion>(modelo);



            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                transaccion.Monto *= -1;
            }

            await repositorioTransacciones.Actualizar(transaccion, modelo.MontoAnterior, modelo.CuentaAnteriorId);

            if (string.IsNullOrEmpty(modelo.urlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(modelo.urlRetorno);
            }

       

        }

        public async Task<IActionResult> Crear()
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var modelo = new TransaccionCreacionViewModel();
            modelo.Cuentas = await ObtenerCuentas(usuarioId);
            modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(TransaccionCreacionViewModel modelo)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            ModelState.Remove("Categorias");
            ModelState.Remove("Categoria");
            ModelState.Remove("Cuentas");
            ModelState.Remove("Cuenta");

            if (!ModelState.IsValid)
            {
                modelo.Cuentas = await ObtenerCuentas(usuarioId);
                modelo.Categorias = await ObtenerCategorias(usuarioId, modelo.TipoOperacionId);
                return View(modelo);
            }

            var cuanta = await repositorioCuentas.ObtenerPorId(modelo.CuentaId, usuarioId);

            if(cuanta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }


            var categoria = await repositorioCategorias.ObtenerPorId(modelo.CategoriaId, usuarioId);

            if(categoria is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }


            modelo.UsuarioId = usuarioId;

            if(modelo.TipoOperacionId == TipoOperacion.Gasto)
            {
                modelo.Monto *= -1;
            }

            await repositorioTransacciones.Crear(modelo);
            return RedirectToAction("Index");

        }

        [HttpPost]
        public async Task<IActionResult> Borrar(int id, string urlRetorno = null)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();

            var transaccion = await repositorioTransacciones.ObtenerPorId(id, usuarioId);

            if(transaccion is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioTransacciones.Borrar(id);

            if (string.IsNullOrEmpty(urlRetorno))
            {
                return RedirectToAction("Index");
            }
            else
            {
                return LocalRedirect(urlRetorno);
            }
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerCuentas(int usuarioId)
        {
            var cuentas = await repositorioCuentas.Buscar(usuarioId);
            return cuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));

        }

        private async Task<IEnumerable<SelectListItem>> ObtenerCategorias(int usuarioId, TipoOperacion tipoOperacion)
        {
            var categorias = await repositorioCategorias.Obtener(usuarioId, tipoOperacion);
            return categorias.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }

        [HttpPost]
        public async Task<IActionResult> ObtenerCategorias([FromBody] TipoOperacion tipoOperacion)
        {
            var usuarioId = servicioUsuarios.ObtenerUsuarioId();
            var categorias = await ObtenerCategorias(usuarioId, tipoOperacion);
            return Ok(categorias);
        }
    }
}
