/**
 * ========================================
 * PAYMENT HISTORY - ENHANCED JAVASCRIPT
 * Pure JavaScript, No Dependencies
 * ========================================
 */

// Initialize on DOM load
document.addEventListener('DOMContentLoaded', function () {
    initializePaymentHistory();
});

/**
 * Main initialization function
 */
function initializePaymentHistory() {
    setupModalListeners();
    setupKeyboardListeners();
    setupClickPropagation();
}

/**
 * Setup modal open/close functionality
 */
function setupModalListeners() {
    // Add click listeners to all transaction cards
    const cards = document.querySelectorAll('.transaction-card');
    cards.forEach((card, index) => {
        card.addEventListener('click', function (e) {
            // Don't open modal if clicking the button directly (handled by button)
            if (!e.target.closest('.view-details-btn')) {
                openModal(index);
            }
        });
    });
}

/**
 * Open modal by index
 */
function openModal(index) {
    const overlay = document.getElementById('modal-overlay-' + index);
    const modal = document.getElementById('modal-' + index);

    if (overlay && modal) {
        // Add active classes
        overlay.classList.add('active');
        modal.classList.add('active');

        // Prevent body scroll
        document.body.style.overflow = 'hidden';

        // Store current modal index for keyboard navigation
        window.currentModalIndex = index;
    }
}

/**
 * Close modal by index
 */
function closeModal(index) {
    const overlay = document.getElementById('modal-overlay-' + index);
    const modal = document.getElementById('modal-' + index);

    if (overlay && modal) {
        // Remove active classes
        overlay.classList.remove('active');
        modal.classList.remove('active');

        // Restore body scroll
        document.body.style.overflow = '';

        // Clear current modal index
        window.currentModalIndex = null;
    }
}

/**
 * Close all open modals
 */
function closeAllModals() {
    const activeModals = document.querySelectorAll('.modal-container.active');
    activeModals.forEach(function (modal) {
        const index = modal.id.replace('modal-', '');
        closeModal(parseInt(index));
    });
}

/**
 * Setup keyboard event listeners
 */
function setupKeyboardListeners() {
    document.addEventListener('keydown', function (e) {
        // ESC key closes modal
        if (e.key === 'Escape' || e.keyCode === 27) {
            closeAllModals();
        }
    });
}

/**
 * Setup click propagation prevention
 */
function setupClickPropagation() {
    // Prevent modal content clicks from closing modal
    const modalContainers = document.querySelectorAll('.modal-container');
    modalContainers.forEach(function (modal) {
        modal.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    });

    // Prevent button clicks from bubbling to card
    const buttons = document.querySelectorAll('.view-details-btn');
    buttons.forEach(function (button) {
        button.addEventListener('click', function (e) {
            e.stopPropagation();
        });
    });
}

/**
 * Format currency for display
 */
function formatCurrency(amount, currency = 'USD') {
    return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: currency,
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(amount);
}

/**
 * Format date for display
 */
function formatDate(dateString) {
    const date = new Date(dateString);
    const options = {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    return date.toLocaleDateString('en-US', options);
}

/**
 * Calculate refund percentage
 */
function calculateRefundPercentage(refundAmount, originalAmount) {
    if (!originalAmount || originalAmount === 0) return 0;
    return Math.round((refundAmount / originalAmount) * 100);
}

/**
 * Animate card entrance on scroll (optional enhancement)
 */
function animateCardsOnScroll() {
    const cards = document.querySelectorAll('.transaction-card');

    const observer = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }, index * 50);
            }
        });
    }, {
        threshold: 0.1
    });

    cards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
        observer.observe(card);
    });
}

/**
 * Print transaction details (optional feature)
 */
function printTransaction(index) {
    const modal = document.getElementById('modal-' + index);
    if (modal) {
        window.print();
    }
}

/**
 * Export transaction to JSON (optional feature)
 */
function exportTransaction(index) {
    // This would need data passed from the server
    console.log('Export transaction:', index);
}

// Make functions globally available for inline event handlers
window.openModal = openModal;
window.closeModal = closeModal;
window.closeAllModals = closeAllModals;
window.printTransaction = printTransaction;
window.exportTransaction = exportTransaction;