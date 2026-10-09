// Confirmación antes de enviar formularios importantes.
// Uso: <form data-confirmar="¿Desea eliminar...?" data-confirmar-boton="Sí, eliminar" data-confirmar-estilo="danger">
(function () {
    const modalEl = document.getElementById('modalConfirmar');
    if (!modalEl || typeof bootstrap === 'undefined') return;

    const modal = new bootstrap.Modal(modalEl);
    const texto = modalEl.querySelector('[data-texto]');
    const boton = modalEl.querySelector('[data-aceptar]');
    let pendiente = null;

    document.addEventListener('submit', function (e) {
        const form = e.target;
        if (!form.dataset || !form.dataset.confirmar || form.dataset.confirmado === 'si') return;

        e.preventDefault();
        pendiente = form;
        texto.textContent = form.dataset.confirmar;
        boton.textContent = form.dataset.confirmarBoton || 'Confirmar';
        boton.className = 'btn btn-' + (form.dataset.confirmarEstilo || 'primary');
        modal.show();
    });

    boton.addEventListener('click', function () {
        if (!pendiente) return;
        pendiente.dataset.confirmado = 'si';
        modal.hide();
        pendiente.requestSubmit ? pendiente.requestSubmit() : pendiente.submit();
    });
})();

// Muestra el nombre del PDF elegido antes de subirlo.
document.querySelectorAll('input[type=file][data-nombre-destino]').forEach(function (input) {
    input.addEventListener('change', function () {
        const destino = document.getElementById(input.dataset.nombreDestino);
        if (destino) destino.textContent = input.files.length ? input.files[0].name : '';
    });
});
