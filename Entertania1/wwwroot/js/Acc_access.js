
        // Add shake animation if user tries to interact
        document.addEventListener('DOMContentLoaded', function() {
            const errorCard = document.querySelector('.error-card');

        // Shake animation on load
        setTimeout(function() {
            errorCard.style.animation = 'slideUp 0.6s ease, shake 0.5s ease 0.6s';
            }, 100);
        });

        // Add shake keyframe
        const style = document.createElement('style');
        style.textContent = `
        @keyframes shake {
            0 %, 100 % { transform: translateX(0); }
                10%, 30%, 50%, 70%, 90% {transform: translateX(-5px); }
        20%, 40%, 60%, 80% {transform: translateX(5px); }
            }
        `;
        document.head.appendChild(style);