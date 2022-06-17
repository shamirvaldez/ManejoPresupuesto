using ManejoPresupuesto.Validaciones;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ManejoPresupuesto.Models
{
    public class TipoCuenta /*: IValidatableObject*/
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "El campo nombre es requerido")]
        [StringLength(maximumLength: 50, MinimumLength = 3, ErrorMessage = "La logintud del campo {0} debe ser entre 3 a 50 caracteres")]
        [Display(Name = "Nombre del tipo cuenta")]

        [PrimeraLetraMayuscula]
        [Remote(action: "VerificarExisteTipoCuenta", controller: "TipoCuentas")]
        public string Nombre { get; set; }
        public int UsuarioId { get; set; }
        public int Orden { get; set; }

        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    if (Nombre != null && Nombre.Length > 0)
        //    {
        //        var primeraLetra = Nombre[0].ToString();

        //        if (primeraLetra != primeraLetra.ToUpper())
        //        {
        //            yield return new ValidationResult("La primera letra debe ser mayusculas",
        //            new[] { nameof(Nombre) });
        //        }
        //    }

            //[Required(ErrorMessage = "El campo nombre es requerido")]
            //[EmailAddress(ErrorMessage = "El campo debe ser un correo electronico valido")]
            //public string Email { get; set; }
            //[Range(minimum: 18, maximum: 130, ErrorMessage = "El valor debe estar entre {1} y {2}")]
            //public int Edad { get; set; }
            //[Url(ErrorMessage = "El campo debe ser una URL valida")]
            //[Required(ErrorMessage = "El campo nombre es requerido")]
            //public string URL { get; set; }
            //[CreditCard(ErrorMessage = "La tarjeta de credito no es valida")]
            //[Required(ErrorMessage = "El campo nombre es requerido")]
            //[Display(Name = "Tarjeta De Credito")]
            //public string TarjetaDeCredtio { get; set; }

        //}
    }
}
