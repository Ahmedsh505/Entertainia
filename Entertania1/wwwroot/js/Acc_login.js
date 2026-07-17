    <partial name="_ValidationScriptsPartial" / >
    $(document).ready(function() {
            // Add focus effects to form controls
            $('.form-control').on('focus', function() {
                $(this).closest('.form-group').addClass('focused');
            }).on('blur', function() {
                $(this).closest('.form-group').removeClass('focused');
            });

            // Auto-dismiss alerts after 8 seconds
            setTimeout(function() {
                $('.alert').fadeOut('slow');
            }, 8000);

            // Form submission animation
            $('form').on('submit', function() {
                var btn = $(this).find('.btn-submit');
                btn.html('<i class="bi bi-arrow-repeat spinning me-2"></i>Signing In...');
                btn.prop('disabled', true);
            });

            // Spinning animation for loading state
            $('<style>')
                .text('.spinning { animation: spin 1s linear infinite; } @keyframes spin { 100% { transform: rotate(360deg); } }')
                .appendTo('head');
        });
