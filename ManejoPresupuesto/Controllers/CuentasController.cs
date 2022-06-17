using AutoMapper;
using ManejoPresupuesto.Models;
using ManejoPresupuesto.Servicios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ManejoPresupuesto.Controllers
{
    public class CuentasController : Controller
    {
        private readonly IRepositorioTiposCuentas repositorioTiposCuentas;
        private readonly IServiciosUsuarios serviciosUsuarios;
        private readonly IRepostiorioCuentas repositorioCuenta;
        private readonly IMapper mapper;
        private readonly IRepositorioTransacciones repositorioTransacciones;
        private readonly IServicioReportes servicioReportes;

        public CuentasController(IRepositorioTiposCuentas repositorioTiposCuentas, IServiciosUsuarios serviciosUsuarios,
            IRepostiorioCuentas cuenta, IMapper mapper, IRepositorioTransacciones repositorioTransacciones, IServicioReportes servicioReportes)
        {
            this.repositorioTiposCuentas = repositorioTiposCuentas;
            this.serviciosUsuarios = serviciosUsuarios;
            this.repositorioCuenta = cuenta;
            this.mapper = mapper;
            this.repositorioTransacciones = repositorioTransacciones;
            this.servicioReportes = servicioReportes;
        }


        public async Task<IActionResult> Index()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            var cuentaConTipoCuenta = await repositorioCuenta.Buscar(usuarioId);

            var modelo =  cuentaConTipoCuenta
                         .GroupBy(x => x.TipoCuenta)
                         .Select(grupo => new IndiceCuentasViewModel
                         {
                             TipoCuenta = grupo.Key,
                             Cuentas = grupo.AsEnumerable()
                         }).ToList();

            return View(modelo);
        }

        public async Task<IActionResult> Detalle(int id, int mes, int ano)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var cuenta = await repositorioCuenta.ObtenerPorId(id, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var modelo = await servicioReportes
                         .ObtenerReporteTransaccionesDetallasPorCuenta(usuarioId, id, mes, ano, ViewBag);
          
            ViewBag.Cuenta = cuenta.Nombre;

            return View(modelo);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
           // var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);
            var modelo = new CuentaCreacionViewModel();
            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId);

            return View(modelo);
        }

        [HttpPost]
        public async Task<IActionResult> Crear(CuentaCreacionViewModel cuentas)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var tipoCuenta = await repositorioTiposCuentas.ObtenerPorId(cuentas.TipoCuentaId, usuarioId);

            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            ModelState.Remove("TipoCuenta");
            ModelState.Remove("TiposCuentas");

            if (!ModelState.IsValid)
            {
                cuentas.TiposCuentas = await ObtenerTiposCuentas(usuarioId);
                return View(cuentas);
            }

            await repositorioCuenta.Crear(cuentas);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Editar(int id)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var cuenta = await repositorioCuenta.ObtenerPorId(id, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }


            var modelo = mapper.Map<CuentaCreacionViewModel>(cuenta);

            modelo.TiposCuentas = await ObtenerTiposCuentas(usuarioId);

            return View(modelo);
         }


        [HttpPost]
        public async Task<IActionResult> Editar(CuentaCreacionViewModel cuentaEditar)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();
            var cuenta = await repositorioCuenta.ObtenerPorId(cuentaEditar.Id, usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            var tipoCuenta = await repositorioCuenta.ObtenerPorId(cuentaEditar.TipoCuentaId, usuarioId);

            if(tipoCuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }


            await repositorioCuenta.Actualizar(cuentaEditar);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Borrar(int id)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            var cuenta = await repositorioCuenta.ObtenerPorId(id,usuarioId);

            if(cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            return View(cuenta);

        }

        [HttpPost]
        public async Task<IActionResult> BorrarCuenta(int id)
        {
            var usuarioId = serviciosUsuarios.ObtenerUsuarioId();

            var cuenta = await repositorioCuenta.ObtenerPorId(id, usuarioId);

            if (cuenta is null)
            {
                return RedirectToAction("NoEncontrado", "Home");
            }

            await repositorioCuenta.Borrar(id);
            return RedirectToAction("Index");
        }
        private async Task<IEnumerable<SelectListItem>> ObtenerTiposCuentas(int usuarioId)
        {
            var tiposCuentas = await repositorioTiposCuentas.Obtener(usuarioId);
            return  tiposCuentas.Select(x => new SelectListItem(x.Nombre, x.Id.ToString()));
        }
    }
}
