// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
// Funzioni generali per il sito
document.addEventListener('DOMContentLoaded', function () {

    // Conferma eliminazione
    const deleteButtons = document.querySelectorAll('.btn-delete');
    deleteButtons.forEach(button => {
        button.addEventListener('click', function (e) {
            if (!confirm('Sei sicuro di voler eliminare questo elemento?')) {
                e.preventDefault();
            }
        });
    });

    // Auto-dismiss alerts
    const alerts = document.querySelectorAll('.alert-dismissible');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000);
    });

    // Form validation migliorata
    const forms = document.querySelectorAll('.needs-validation');
    forms.forEach(form => {
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    // Toggle password visibility
    const togglePassword = document.querySelector('.toggle-password');
    if (togglePassword) {
        togglePassword.addEventListener('click', function () {
            const passwordInput = document.querySelector('#Password');
            const type = passwordInput.getAttribute('type') === 'password' ? 'text' : 'password';
            passwordInput.setAttribute('type', type);
            this.classList.toggle('bi-eye');
            this.classList.toggle('bi-eye-slash');
        });
    }
});

// Funzioni per gestione stati
function updateVehicleStatus(vehicleId, newStatus) {
    // Qui andrà la chiamata API per aggiornare lo stato
    console.log(`Aggiornamento veicolo ${vehicleId} a stato: ${newStatus}`);

    // Aggiornamento UI temporaneo
    const statusBadge = document.querySelector(`#status-badge-${vehicleId}`);
    if (statusBadge) {
        statusBadge.className = `badge badge-${newStatus.toLowerCase()}`;
        statusBadge.textContent = newStatus;
    }
}