function inicializarFormularioTransacciones() {
 
            $("#TipoOperacionId").change(async function () {
                const valorSeleccionado = $(this).val();

                const respuesta = await fetch(urlObtenerCategoria, {
                    method: 'POST',
                    body: valorSeleccionado,
                    headers: {
                        'Content-type': 'application/json'
                    }
                });

                const json = await respuesta.json();
                const opciones =
                    json.map(categoria => `<option value=${categoria.value}>${categoria.text}</option>`);
                $("#CategoriaId").html(opciones);
            })
}